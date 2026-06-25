namespace Telemetry.Publisher.Controllers;

using Microsoft.AspNetCore.Mvc;
using Telemetry.Publisher.Services;

[ApiController]
[Route("api/telemetry")]
public class TelemetryController : ControllerBase
{
    private readonly ITelemetryQueueManager _queueManager;

    public TelemetryController(ITelemetryQueueManager queueManager)
    {
        _queueManager = queueManager;
    }

    [HttpGet("next")]
    public async Task<IActionResult> GetNext()
    {
        var reading = await _queueManager.DequeueAsync();
        return reading != null ? Ok(reading) : NoContent(); 
                
    }

    [HttpGet("stats")]
    public IActionResult GetStats()
    {
        var (queueDepth, totalGenerated, totalSpilled) = _queueManager.GetStats();
        return Ok(new
        {
            QueueDepth = queueDepth,
            TotalGenerated = totalGenerated,
            TotalSpilled = totalSpilled
        });
    }

    [HttpGet("health")]
    public IActionResult GetHealth()
    {
        return Ok(new { Status = "Healthy", EngineTime = DateTime.UtcNow });
    }
}