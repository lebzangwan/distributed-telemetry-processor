namespace Telemetry.Publisher.Repository;

using Telemetry.Shared.Models;

public interface IDatabaseRepository
{
    Task SavePendingReadingAsync(SensorReading reading);
    Task<SensorReading?> GetOldestPendingAsync();
}