namespace Telemetry.Publisher.Services;

using Telemetry.Shared.Models;

public interface ITelemetryQueueManager
{
    void Enqueue(SensorReading reading);
    Task<SensorReading?> DequeueAsync();
    (long QueueDepth, long TotalGenerated, long TotalSpilled) GetStats();
}