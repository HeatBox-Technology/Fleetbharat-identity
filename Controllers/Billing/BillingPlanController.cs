using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/billing/plans")]
public class BillingPlanController : ControllerBase
{
    private readonly IBillingPlanService _service;

    public BillingPlanController(IBillingPlanService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int skip = 0, [FromQuery] int take = 50, CancellationToken ct = default)
    {
        var data = await _service.GetPlansAsync(skip, take, ct);
        return Ok(ApiResponse<object>.Ok(data));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct = default)
    {
        var data = await _service.GetPlanByIdAsync(id, ct);
        if (data == null)
        {
            return NotFound(ApiResponse<object>.Fail("Plan not found", 404));
        }

        return Ok(ApiResponse<object>.Ok(data));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePlanDto dto, CancellationToken ct = default)
    {
        var id = await _service.CreatePlanAsync(dto, ct);
        return Ok(ApiResponse<object>.Ok(new { id }, "Plan created", 200));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePlanDto dto, CancellationToken ct = default)
    {
        var ok = await _service.UpdatePlanAsync(id, dto, ct);
        if (!ok)
        {
            return NotFound(ApiResponse<object>.Fail("Plan not found", 404));
        }

        return Ok(ApiResponse<object>.Ok(null, "Plan updated", 200));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct = default)
    {
        var ok = await _service.DeletePlanAsync(id, ct);
        if (!ok)
        {
            return NotFound(ApiResponse<object>.Fail("Plan not found", 404));
        }

        return Ok(ApiResponse<object>.Ok(null, "Plan deleted", 200));
    }

    [HttpPost("{planId:int}/features")]
    public async Task<IActionResult> UpsertFeatures(int planId, [FromBody] PlanFeatureUpsertDto dto, CancellationToken ct = default)
    {
        var ok = await _service.UpsertPlanFeaturesAsync(planId, dto, ct);
        if (!ok)
        {
            return NotFound(ApiResponse<object>.Fail("Plan not found", 404));
        }

        return Ok(ApiResponse<object>.Ok(null, "Plan features updated", 200));
    }
}
