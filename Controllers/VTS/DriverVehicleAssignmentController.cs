using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/driver-vehicle-assignment")]
public class DriverVehicleAssignmentController : ControllerBase
{
    private readonly IDriverVehicleAssignmentService _service;

    public DriverVehicleAssignmentController(IDriverVehicleAssignmentService service)
    {
        _service = service;
    }

    [HttpPost("Create")]
    public async Task<IActionResult> Create([FromBody] CreateDriverVehicleAssignmentDto dto)
    {
        var id = await _service.CreateAsync(dto);

        return Ok(ApiResponse<object>.Ok(
            new { driverVehicleAssignmentId = id },
            "Driver vehicle assignment created",
            200));
    }

    [HttpPut("Update/{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateDriverVehicleAssignmentDto dto)
    {
        var ok = await _service.UpdateAsync(id, dto);

        if (!ok)
            return NotFound(ApiResponse<object>.Fail("Driver vehicle assignment not found", 404));

        return Ok(ApiResponse<string>.Ok(
            "Updated",
            "Driver vehicle assignment updated",
            200));
    }

    [HttpGet("GetAll")]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] int? accountContextId = null,
        [FromQuery] string? search = null)
    {
        var result = await _service.GetAllAsync(page, pageSize, accountContextId, search);

        return Ok(ApiResponse<object>.Ok(result, "Success", 200));
    }

    [HttpGet("GetById/{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var data = await _service.GetByIdAsync(id);

        if (data == null)
            return NotFound(ApiResponse<object>.Fail("Driver vehicle assignment not found", 404));

        return Ok(ApiResponse<object>.Ok(data, "Success", 200));
    }

    [HttpDelete("Delete/{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var ok = await _service.DeleteAsync(id);

        if (!ok)
            return NotFound(ApiResponse<object>.Fail("Driver vehicle assignment not found", 404));

        return Ok(ApiResponse<string>.Ok(
            "Deleted",
            "Driver vehicle assignment deleted",
            200));
    }

    [HttpPatch("SoftDelete/{id}")]
    public async Task<IActionResult> SoftDelete(int id)
    {
        var ok = await _service.SoftDeleteAsync(id);

        if (!ok)
            return NotFound(ApiResponse<object>.Fail("Driver vehicle assignment not found", 404));

        return Ok(ApiResponse<string>.Ok(
            "Deleted",
            "Driver vehicle assignment soft deleted",
            200));
    }
}
