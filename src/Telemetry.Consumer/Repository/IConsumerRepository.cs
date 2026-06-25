namespace Telemetry.Consumer.Repository;

using Telemetry.Shared.Models;

public interface IConsumerRepository
{
    Task InsertAnalysisResultAsync(AnalysisResult result);
}