using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/customers/{customerId}/plans")]
public class CustomerPlansController : ControllerBase
{
    private readonly ICustomerPlanService _service;

    public CustomerPlansController(ICustomerPlanService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Assign(Guid customerId, [FromBody] AssignPlanToCustomerDto dto)
    {
        if (dto.PlanId == Guid.Empty)
            return BadRequest(new { message = "PlanId is required" });

        var id = await _service.AssignAsync(customerId, dto);
        return Ok(new { assignmentId = id });
    }

    [HttpGet]
    public async Task<IActionResult> GetAssigned(Guid customerId)
    {
        var data = await _service.GetAssignedAsync(customerId);
        if (data == null) return NotFound(new { message = "No plan assigned" });

        return Ok(data);
    }
}