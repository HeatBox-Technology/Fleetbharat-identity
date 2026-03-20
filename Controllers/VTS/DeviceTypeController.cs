using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

[ApiController]
[Route("api/device-types")]
public class DeviceTypeController : ControllerBase
{
    private readonly IDeviceTypeService _service;

    public DeviceTypeController(IDeviceTypeService service)
    {
        _service = service;
    }

    /// <summary>
    /// Create device type
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDeviceTypeDto req)
    {
        var id = await _service.CreateAsync(req);

        return Ok(ApiResponse<object>.Ok(
            new { deviceTypeId = id },
            "Device type created",
            200));
    }

    /// <summary>
    /// Get device types with summary + pagination
    /// </summary>
    [HttpGet("list")]
    public async Task<IActionResult> GetDeviceTypes(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null)
    {
        var result = await _service.GetDeviceTypes(page, pageSize, search);

        return Ok(ApiResponse<object>.Ok(result, "Success", 200));
    }

    /// <summary>
    /// Get paged only
    /// </summary>
    [HttpGet("paged")]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null)
    {
        var result = await _service.GetPagedAsync(page, pageSize, search);

        return Ok(ApiResponse<object>.Ok(result, "Success", 200));
    }


    /// <summary>
    /// Get device type by id
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var data = await _service.GetByIdAsync(id);

        if (data == null)
            return NotFound(ApiResponse<object>.Fail("Device type not found", 404));

        return Ok(ApiResponse<object>.Ok(data, "Success", 200));
    }

    /// <summary>
    /// Update device type
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateDeviceTypeDto req)
    {
        var ok = await _service.UpdateAsync(id, req);

        if (!ok)
            return NotFound(ApiResponse<object>.Fail("Device type not found", 404));

        return Ok(ApiResponse<string>.Ok("Updated", "Device type updated", 200));
    }

    /// <summary>
    /// Enable / Disable device type
    /// </summary>
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(
        int id,
        [FromQuery] bool isEnabled)
    {
        var ok = await _service.UpdateStatusAsync(id, isEnabled);

        if (!ok)
            return NotFound(ApiResponse<object>.Fail("Device type not found", 404));

        return Ok(ApiResponse<string>.Ok("Updated", "Status updated", 200));
    }

    /// <summary>
    /// Soft delete device type
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var ok = await _service.DeleteAsync(id);

        if (!ok)
            return NotFound(ApiResponse<object>.Fail("Device type not found", 404));

        return Ok(ApiResponse<string>.Ok("Deleted", "Device type deleted", 200));
    }

    /// <summary>
    /// Bulk create device types
    /// </summary>
    [HttpPost("bulk")]
    public async Task<IActionResult> BulkUpload([FromBody] List<CreateDeviceTypeDto> items)
    {
        var result = await _service.BulkCreateAsync(items);

        return Ok(ApiResponse<object>.Ok(
            result,
            "Device types created successfully",
            200));
    }

}