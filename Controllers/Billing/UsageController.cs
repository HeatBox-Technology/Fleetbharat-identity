using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/billing/usage")]
public class UsageController : ControllerBase
{
    private readonly IBillingUsageService _service;

    public UsageController(IBillingUsageService service)
    {
        _service = service;
    }

    [HttpPost("record")]
    public async Task<IActionResult> Record([FromBody] UsageRecordCreateDto dto, CancellationToken ct = default)
    {
        var id = await _service.RecordUsageAsync(dto, ct);
        return Ok(ApiResponse<object>.Ok(new { id }, "Usage recorded", 200));
    }

    [HttpGet("{accountId:int}")]
    public async Task<IActionResult> GetByAccount(int accountId, [FromQuery] int skip = 0, [FromQuery] int take = 100, CancellationToken ct = default)
    {
        var data = await _service.GetUsageByAccountAsync(accountId, skip, take, ct);
        return Ok(ApiResponse<object>.Ok(data));
    }
}
