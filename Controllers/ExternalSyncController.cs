using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;

[ApiController]
[Route("api/external-sync")]
public class ExternalSyncController : ControllerBase
{
    private readonly IExternalSyncDashboardService _dashboardService;
    private readonly IExternalSyncQueueService _queueService;

    public ExternalSyncController(
        IExternalSyncDashboardService dashboardService,
        IExternalSyncQueueService queueService)
    {
        _dashboardService = dashboardService;
        _queueService = queueService;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken ct)
    {
        var data = await _dashboardService.GetStatsAsync(ct);
        return Ok(ApiResponse<object>.Ok(data, "Success", 200));
    }

    [HttpGet("failed")]
    public async Task<IActionResult> GetFailed([FromQuery] int take = 100, CancellationToken ct = default)
    {
        var data = await _dashboardService.GetFailedAsync(take, ct);
        return Ok(ApiResponse<object>.Ok(data, "Success", 200));
    }

    [HttpGet("dlq")]
    public async Task<IActionResult> GetDlq([FromQuery] int take = 100, CancellationToken ct = default)
    {
        var data = await _dashboardService.GetDlqAsync(take, ct);
        return Ok(ApiResponse<object>.Ok(data, "Success", 200));
    }

    [HttpPost("retry/{queueId:long}")]
    public async Task<IActionResult> RetryFailed(long queueId, CancellationToken ct)
    {
        var ok = await _dashboardService.RetryFailedAsync(queueId, ct);
        if (!ok) return NotFound(ApiResponse<object>.Fail("Failed record not found", 404));
        return Ok(ApiResponse<object>.Ok(null, "Queued for retry", 200));
    }

    [HttpPost("dlq/{dlqId:long}/reprocess")]
    public async Task<IActionResult> ReprocessDlq(long dlqId, CancellationToken ct)
    {
        var ok = await _dashboardService.ReprocessDlqAsync(dlqId, ct);
        if (!ok) return NotFound(ApiResponse<object>.Fail("DLQ record not found", 404));
        return Ok(ApiResponse<object>.Ok(null, "DLQ record re-queued", 200));
    }

    [HttpPost("enqueue")]
    public async Task<IActionResult> Enqueue([FromBody] ExternalSyncQueueCreateRequest request, CancellationToken ct)
    {
        await _queueService.EnqueueAsync(request, ct);
        return Ok(ApiResponse<object>.Ok(null, "Sync item queued", 200));
    }
}
