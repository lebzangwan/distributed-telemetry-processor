namespace Telemetry.Shared.Models;

public class AnalysisResult
{
    public string Id { get; set; } = string.Empty; 
    public string SensorReadingId { get; set; } = string.Empty; 
    public string AnalysisType { get; set; } = string.Empty; 
    public double Result { get; set; }
    public DateTime ProcessedAt { get; set; }
}