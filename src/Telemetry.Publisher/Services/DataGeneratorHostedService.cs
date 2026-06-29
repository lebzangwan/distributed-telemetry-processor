namespace Telemetry.Publisher.Services;

using Telemetry.Shared.Models;

public partial class DataGeneratorHostedService : BackgroundService
{
    private readonly ITelemetryQueueManager _queueManager;
    private readonly ILogger<DataGeneratorHostedService> _logger;
    private readonly string[] _sensorTypes = ["Temperature", "Pressure", "Humidity", "Vibration"];
    private long _sequenceId = 10000;

    public DataGeneratorHostedService(ITelemetryQueueManager queueManager, ILogger<DataGeneratorHostedService> logger)
    {
        _queueManager = queueManager;
        _logger = logger; 
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var nextId = Interlocked.Increment(ref _sequenceId);
            
            var reading = new SensorReading
            {
                Id = $"sr_{nextId}",
                Timestamp = DateTime.UtcNow,
                Value = Math.Round(Random.Shared.NextDouble() * 100, 3),
                SensorType = _sensorTypes[Random.Shared.Next(_sensorTypes.Length)]
            };

            LogReadingGenerated(reading.Id, reading.Value);

            _queueManager.Enqueue(reading);

            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Generated Sensor Reading: {Id} | Value: {Value}")]
    private partial void LogReadingGenerated(string id, double value);
}