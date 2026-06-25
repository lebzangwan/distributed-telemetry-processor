namespace Telemetry.Tests;

using Microsoft.Extensions.Logging;
using Moq;
using Telemetry.Publisher.Repository;
using Telemetry.Publisher.Services;
using Telemetry.Shared.Models;
using Xunit;

public class TelemetryTests
{
    [Fact]
    public async Task DequeueAsync_ShouldPrioritizeDatabase_WhenDatabaseHasData()
    {
       var mockRepo = new Mock<IDatabaseRepository>();
        var mockLogger = new Mock<ILogger<TelemetryQueueManager>>();

        var dbReading = new SensorReading { Id = "sr_db_101", Value = 99.9, SensorType = "Pressure", Timestamp = DateTime.UtcNow.AddMinutes(-5) };
        var memoryReading = new SensorReading { Id = "sr_mem_102", Value = 12.5, SensorType = "Pressure", Timestamp = DateTime.UtcNow };

       mockRepo.Setup(r => r.GetOldestPendingAsync()).ReturnsAsync(dbReading);

        var queueManager = new TelemetryQueueManager(mockRepo.Object, mockLogger.Object);
        
        queueManager.Enqueue(memoryReading);

        var result = await queueManager.DequeueAsync();

        Assert.NotNull(result);
        Assert.Equal("sr_db_101", result.Id);
        mockRepo.Verify(r => r.GetOldestPendingAsync(), Times.Once);
    }
}