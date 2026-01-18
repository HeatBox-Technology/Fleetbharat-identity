using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/plans")]
public class PlansController : ControllerBase
{
    private readonly IPlanService _service;

    public PlansController(IPlanService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateMarketPlanDto dto)
    {
        var id = await _service.CreateAsync(dto);
        return Ok(ApiResponse<object>.Ok(new { planId = id }, "Plan created", 200));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var plan = await _service.GetByIdAsync(id);
        if (plan == null) return NotFound(ApiResponse<string>.Fail("Plan not found", 400));

        return Ok(ApiResponse<object>.Ok(plan, "Plan details", 200));
    }

    // ✅ Pagination + Filters
    [HttpGet]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? tenantCategory = null,
        [FromQuery] string? billingCycle = null,
        [FromQuery] string? pricingModel = null,
        [FromQuery] bool? isActive = null)
    {
        var pageRequest = new PagedRequestDto { Page = page, PageSize = pageSize };
        var filter = new PlanFilterDto
        {
            Search = search,
            TenantCategory = tenantCategory,
            BillingCycle = billingCycle,
            PricingModel = pricingModel,
            IsActive = isActive
        };

        var result = await _service.GetPagedAsync(pageRequest, filter);
        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateMarketPlanDto dto)
    {
        var ok = await _service.UpdateAsync(id, dto);
        if (!ok) return NotFound(ApiResponse<string>.Fail("Plan not found", 400));

        return Ok(ApiResponse<string>.Ok("Updated", "Plan updated", 200));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var ok = await _service.DeleteAsync(id);
        if (!ok) return NotFound(ApiResponse<string>.Fail("Plan not found", 400));

        return Ok(ApiResponse<string>.Ok("Deleted", "Plan deleted", 200));
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromQuery] bool isActive)
    {
        var ok = await _service.UpdateStatusAsync(id, isActive);
        if (!ok) return NotFound(ApiResponse<string>.Fail("Plan not found", 400));

        return Ok(ApiResponse<string>.Ok("Updated", "Plan status updated", 200));
    }
}