namespace Telemetry.Publisher.Services;

using Telemetry.Shared.Models;

public class DataGeneratorHostedService : BackgroundService
{
    private readonly ITelemetryQueueManager _queueManager;
    private readonly ILogger<DataGeneratorHostedService> _logger;
    private readonly Random _random = new();
    private readonly string[] _sensorTypes = { "Temperature", "Pressure", "Humidity", "Vibration" };
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
                Value = Math.Round(_random.NextDouble() * 100, 3),
                SensorType = _sensorTypes[_random.Next(_sensorTypes.Length)]
            };

            if (_logger != null && _logger.IsEnabled(LogLevel.Information)) { _logger.LogInformation("Generated Sensor Reading: {Id} | Value: {Value}", reading.Id, reading.Value); }
            _queueManager.Enqueue(reading);

            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
        }
    }
}