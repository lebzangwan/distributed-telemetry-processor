namespace Telemetry.Shared.Models;

public class SensorReading {
    public string Id { get; set; } = String.Empty;
    public DateTime Timestamp { get; set; }

    public double Value { get; set; }

    public string SensorType { get; set; } = String.Empty;
}