namespace Telemetry.Consumer.Services;

using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Threading.Channels;
using Telemetry.Consumer.Repository;
using Telemetry.Shared.Models;

public partial class TelemetryProcessorWorker : BackgroundService
{
    private readonly HttpClient _httpClient;
    private readonly IConsumerRepository _repository;
    private readonly ILogger<TelemetryProcessorWorker> _logger;
    private readonly Channel<SensorReading> _processingChannel;
    private readonly string _publisherUrl;

    // IDE0028 Fix: Simplified dictionary collection initialization expression ([])
    private readonly Dictionary<string, List<double>> _historicalBuffer = [];

    public TelemetryProcessorWorker(
        HttpClient httpClient,
        IConsumerRepository repository,
        IConfiguration configuration,
        ILogger<TelemetryProcessorWorker> logger)
    {
        _httpClient = httpClient;
        _repository = repository;
        _logger = logger;
        _publisherUrl = configuration["PublisherUrl"] ?? "http://localhost:5200/api/telemetry/next";

        _processingChannel = Channel.CreateUnbounded<SensorReading>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true
        });
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var processingTask = Task.Run(() => StartCalculationEngineAsync(stoppingToken), stoppingToken);

        int retryDelaySeconds = 1;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var response = await _httpClient.GetAsync(_publisherUrl, stoppingToken);

                if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    retryDelaySeconds = 1;
                    await Task.Delay(TimeSpan.FromMilliseconds(500), stoppingToken);
                    continue;
                }

                if (response.IsSuccessStatusCode)
                {
                    retryDelaySeconds = 1;
                    var reading = await response.Content.ReadFromJsonAsync<SensorReading>(cancellationToken: stoppingToken);
                    if (reading != null)
                    {
                        LogReadingFetched(reading.Id);
                        await _processingChannel.Writer.WriteAsync(reading, stoppingToken);
                    }
                }
                else
                {
                    throw new HttpRequestException($"Server returned non-success code: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                LogPublisherOffline(retryDelaySeconds, ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(retryDelaySeconds), stoppingToken);

                retryDelaySeconds = Math.Min(retryDelaySeconds * 2, 60);
            }
        }

        _processingChannel.Writer.Complete();
        await processingTask;
    }

    private async Task StartCalculationEngineAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Complex Processing Thread initialized completely.");

        await foreach (var reading in _processingChannel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                var correlationId = Guid.NewGuid().ToString()[..8];

                // CA1873 Fix: Removed direct string logging interpolation execution paths
                LogCalculationsStarted(correlationId, reading.Id);

                // CA1854 Fix: Using TryGetValue eliminates double-lookup dictionary traversal overhead
                // IDE0028 Fix: Instantiating fallback empty collections natively via target expressions ([])
                if (!_historicalBuffer.TryGetValue(reading.SensorType, out var historicalList))
                {
                    historicalList = [];
                    _historicalBuffer[reading.SensorType] = historicalList;
                }

                historicalList.Add(reading.Value);
                if (historicalList.Count > 5)
                {
                    historicalList.RemoveAt(0);
                }

                double calculatedMovingAverage = historicalList.Average();

                var result = new AnalysisResult
                {
                    Id = $"ar_{Guid.NewGuid():N}",
                    SensorReadingId = reading.Id,
                    AnalysisType = "5-Point Moving Average",
                    Result = Math.Round(calculatedMovingAverage, 3),
                    ProcessedAt = DateTime.UtcNow
                };

                await _repository.InsertAnalysisResultAsync(result);

                LogAnalysisSaved(correlationId, result.Id, reading.Id);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurring during analysis extraction calculation process processing loop.");
            }
        }
    }

    // This creates compile-time ultra-fast logging extensions
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Fetched reading {ReadingId} successfully. Sending to processing channel.")]
    private partial void LogReadingFetched(string readingId);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Publisher service offline or unreachable. Backing off for {Time}s. Error: {Msg}")]
    private partial void LogPublisherOffline(int time, string msg);

    [LoggerMessage(EventId = 3, Level = LogLevel.Information, Message = "[CorrId: {Corr}] Starting complex calculations for reading {ReadingId}")]
    private partial void LogCalculationsStarted(string corr, string readingId);

    [LoggerMessage(EventId = 4, Level = LogLevel.Information, Message = "[CorrId: {Corr}] Saved AnalysisResult {ArId} for reading {ReadingId} successfully to persistence.")]
    private partial void LogAnalysisSaved(string corr, string arId, string readingId);
}