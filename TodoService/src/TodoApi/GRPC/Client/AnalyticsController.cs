using Microsoft.AspNetCore.Mvc;

namespace TodoApi.GRPC.Client;

[ApiController]
[Route("api/[controller]")]
public class AnalyticsController(
    ITodoAnalyticsClient analyticsClient) : ControllerBase
{
    private readonly ITodoAnalyticsClient _analyticsClient = analyticsClient ?? throw new ArgumentNullException(nameof(analyticsClient));
    
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var (activeTasks, completedTasks) = await _analyticsClient.ReturnStats();
        
        return Ok(new
        {
            ActiveTasks = activeTasks,
            CompletedTasks = completedTasks
        });
    }
}