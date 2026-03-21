using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/billing/subscriptions")]
public class SubscriptionController : ControllerBase
{
    private readonly IBillingSubscriptionService _service;

    public SubscriptionController(IBillingSubscriptionService service)
    {
        _service = service;
    }

    [HttpPost("map-plan")]
    public async Task<IActionResult> MapPlan([FromBody] AccountSubscriptionMapPlanDto dto, CancellationToken ct = default)
    {
        var id = await _service.MapPlanAsync(dto, ct);
        return Ok(ApiResponse<object>.Ok(new { id }, "Subscription created", 200));
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int skip = 0, [FromQuery] int take = 50, CancellationToken ct = default)
    {
        var data = await _service.GetSubscriptionsAsync(skip, take, ct);
        return Ok(ApiResponse<object>.Ok(data));
    }

    [HttpGet("{accountId:int}")]
    public async Task<IActionResult> GetByAccount(int accountId, [FromQuery] int skip = 0, [FromQuery] int take = 50, CancellationToken ct = default)
    {
        var data = await _service.GetSubscriptionsByAccountAsync(accountId, skip, take, ct);
        return Ok(ApiResponse<object>.Ok(data));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct = default)
    {
        var ok = await _service.DeleteSubscriptionAsync(id, ct);
        if (!ok)
        {
            return NotFound(ApiResponse<object>.Fail("Subscription not found", 404));
        }

        return Ok(ApiResponse<object>.Ok(null, "Subscription deleted", 200));
    }
}
