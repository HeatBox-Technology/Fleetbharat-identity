using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _service;

    public DashboardController(IDashboardService service)
    {
        _service = service;
    }

    [HttpPost("summary")]
    public async Task<IActionResult> Summary([FromBody] DashboardSummaryRequestDto request, CancellationToken ct = default)
    {
        var data = await _service.GetSummaryAsync(request, ct);
        return Ok(ApiResponse<object>.Ok(data, "Success", 200));
    }
}
