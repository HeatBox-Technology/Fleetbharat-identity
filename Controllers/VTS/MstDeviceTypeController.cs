using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/device-type")]
[Authorize]
public class MstDeviceTypeController : ControllerBase
{
    private readonly IMstDeviceTypeService _service;

    public MstDeviceTypeController(IMstDeviceTypeService service)
    {
        _service = service;
    }

    /// <summary>
    /// Create a new device type.
    /// </summary>
    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] CreateMstDeviceTypeRequestDto request)
    {
        var id = await _service.CreateAsync(request);

        return Ok(ApiResponse<object>.Ok(
            new { deviceTypeId = id },
            "Device type created",
            200));
    }

    /// <summary>
    /// Get all active and non-deleted device types.
    /// </summary>
    [HttpGet("get-all")]
    public async Task<IActionResult> GetAll([FromQuery] string? search = null)
    {
        var result = await _service.GetAllAsync(search);
        return Ok(ApiResponse<object>.Ok(result, "Success", 200));
    }

    /// <summary>
    /// Get an active and non-deleted device type by id.
    /// </summary>
    [HttpGet("get-by-id/{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _service.GetByIdAsync(id);

        if (result == null)
            return NotFound(ApiResponse<object>.Fail("Device type not found", 404));

        return Ok(ApiResponse<object>.Ok(result, "Success", 200));
    }

    /// <summary>
    /// Update an existing device type.
    /// </summary>
    [HttpPut("update/{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateMstDeviceTypeRequestDto request)
    {
        var updated = await _service.UpdateAsync(id, request);

        if (!updated)
            return NotFound(ApiResponse<object>.Fail("Device type not found", 404));

        return Ok(ApiResponse<string>.Ok("Updated", "Device type updated", 200));
    }

    /// <summary>
    /// Soft delete a device type.
    /// </summary>
    [HttpDelete("delete/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _service.DeleteAsync(id);

        if (!deleted)
            return NotFound(ApiResponse<object>.Fail("Device type not found", 404));

        return Ok(ApiResponse<string>.Ok("Deleted", "Device type deleted", 200));
    }
}
