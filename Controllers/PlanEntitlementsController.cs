using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/plans/{planId}/entitlements")]
public class PlanEntitlementsController : ControllerBase
{
    private readonly IPlanEntitlementService _service;

    public PlanEntitlementsController(IPlanEntitlementService service)
    {
        _service = service;
    }

    [HttpPut]
    public async Task<IActionResult> Assign(Guid planId, [FromBody] AssignEntitlementsToPlanDto dto)
    {
        var ok = await _service.AssignAsync(planId, dto.FeatureIds);
        if (!ok) return NotFound(new { message = "Plan not found" });

        return Ok(new { message = "Entitlements updated" });
    }

    [HttpGet]
    public async Task<IActionResult> Get(Guid planId)
    {
        var list = await _service.GetFeatureIdsAsync(planId);
        return Ok(list);
    }
}