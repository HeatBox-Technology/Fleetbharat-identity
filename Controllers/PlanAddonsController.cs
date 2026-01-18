
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/plans/{planId}/addons")]
public class PlanAddonsController : ControllerBase
{
    private readonly IPlanAddonService _service;

    public PlanAddonsController(IPlanAddonService service)
    {
        _service = service;
    }

    [HttpPut]
    public async Task<IActionResult> Assign(Guid planId, [FromBody] AssignAddonsToPlanDto dto)
    {
        var ok = await _service.AssignAsync(planId, dto.AddonIds);
        if (!ok) return NotFound(new { message = "Plan not found" });

        return Ok(new { message = "Addons updated" });
    }

    [HttpGet]
    public async Task<IActionResult> Get(Guid planId)
    {
        var list = await _service.GetAddonIdsAsync(planId);
        return Ok(list);
    }
}