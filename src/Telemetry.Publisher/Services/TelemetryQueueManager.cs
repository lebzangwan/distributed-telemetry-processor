namespace Telemetry.Publisher.Services;

using System.Collections.Concurrent;
using Telemetry.Publisher.Repository;
using Telemetry.Shared.Models;

public class TelemetryQueueManager : ITelemetryQueueManager
{
    private readonly ConcurrentQueue<(SensorReading Reading, DateTime EnqueuedAt)> _queue = new();
    private readonly IDatabaseRepository _dbRepository;
    private readonly ILogger<TelemetryQueueManager> _logger;
    private readonly int _maxCapacity = 10;
    private readonly TimeSpan _maxAge = TimeSpan.FromSeconds(5);
    private long _totalGenerated;
    private long _totalSpilled;
    public TelemetryQueueManager(IDatabaseRepository dbRepository, ILogger<TelemetryQueueManager> logger)
    {
        _dbRepository = dbRepository;
        _logger = logger;
    }

    public void Enqueue(SensorReading reading)
    {
        Interlocked.Increment(ref _totalGenerated);
        _queue.Enqueue((reading, DateTime.UtcNow));
        EvaluateSpillOverConditions();
    }
    public async Task<SensorReading?> DequeueAsync()
    {
        EvaluateSpillOverConditions();

        var dbItem = await _dbRepository.GetOldestPendingAsync();
        if (dbItem != null)
        {
            return dbItem;
        }

        if (_queue.TryDequeue(out var memoryNode))
        {
            return memoryNode.Reading;
        }
        return null;
    }
    private void EvaluateSpillOverConditions()
    {
        while (_queue.Count > _maxCapacity)
        {
            SpillOldestToDatabase();
        }
        while (_queue.TryPeek(out var oldest) && (DateTime.UtcNow - oldest.EnqueuedAt) > _maxAge)
        {
            SpillOldestToDatabase();
        }
    }
    private void SpillOldestToDatabase()
    {
        if (_queue.TryDequeue(out var node))
        {
            Interlocked.Increment(ref _totalSpilled);

            Task.Run(async () => await _dbRepository.SavePendingReadingAsync(node.Reading)).ContinueWith(task =>
           {
               if (task.IsFaulted && task.Exception != null)
               {
                   _logger.LogError(task.Exception.Flatten(), "Asynchronous background task failed to spill reading {Id} to persistent storage.", node.Reading.Id);
               }
           }, TaskContinuationOptions.OnlyOnFaulted);
        }
    }
    public (long QueueDepth, long TotalGenerated, long TotalSpilled) GetStats() =>
    (
       _queue.Count, Interlocked.Read(ref _totalGenerated), Interlocked.Read(ref _totalSpilled));
}