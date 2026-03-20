using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/billing/analytics")]
public class BillingAnalyticsController : ControllerBase
{
    private readonly IBillingAnalyticsService _service;

    public BillingAnalyticsController(IBillingAnalyticsService service)
    {
        _service = service;
    }

    [HttpGet("revenue")]
    public async Task<IActionResult> Revenue(CancellationToken ct = default)
    {
        var data = await _service.GetRevenueProjectionAsync(ct);
        return Ok(ApiResponse<object>.Ok(data));
    }

    [HttpGet("market")]
    public async Task<IActionResult> Market(CancellationToken ct = default)
    {
        var data = await _service.GetMarketPenetrationAsync(ct);
        return Ok(ApiResponse<object>.Ok(data));
    }
}
