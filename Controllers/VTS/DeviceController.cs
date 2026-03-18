using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/devices")]

//[Authorize] // Enable if authentication required
public class DeviceController : ControllerBase
{
    private readonly IDeviceService _service;

    public DeviceController(IDeviceService service)
    {
        _service = service;
    }

    /// <summary>
    /// Create device
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDeviceDto req)
    {
        var id = await _service.CreateAsync(req);

        return Ok(ApiResponse<object>.Ok(
            new { deviceId = id },
            "Device created",
            200));
    }

    /// <summary>
    /// Get devices with summary + pagination
    /// </summary>
    [HttpGet("list")]
    public async Task<IActionResult> GetDevices(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] int? accountId = null,
        [FromQuery] string? search = null)
    {
        var result = await _service.GetDevices(page, pageSize, accountId, search);

        return Ok(ApiResponse<object>.Ok(result, "Success", 200));
    }

    /// <summary>
    /// Get device by Id
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var data = await _service.GetByIdAsync(id);

        if (data == null)
            return NotFound(ApiResponse<object>.Fail("Device not found", 404));

        return Ok(ApiResponse<object>.Ok(data, "Success", 200));
    }

    /// <summary>
    /// Update device
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateDeviceDto req)
    {
        var ok = await _service.UpdateAsync(id, req);

        if (!ok)
            return NotFound(ApiResponse<object>.Fail("Device not found", 404));

        return Ok(ApiResponse<string>.Ok("Updated", "Device updated", 200));
    }

    /// <summary>
    /// Update device status
    /// </summary>
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(
        int id,
        [FromQuery] string status)
    {
        var ok = await _service.UpdateStatusAsync(id, status);

        if (!ok)
            return NotFound(ApiResponse<object>.Fail("Device not found", 404));

        return Ok(ApiResponse<string>.Ok("Updated", "Status updated", 200));
    }

    /// <summary>
    /// Delete device (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var ok = await _service.DeleteAsync(id);

        if (!ok)
            return NotFound(ApiResponse<object>.Fail("Device not found", 404));

        return Ok(ApiResponse<string>.Ok("Deleted", "Device deleted", 200));
    }

    /// <summary>
    /// Get paged devices (without summary)
    /// </summary>
    [HttpGet("paged")]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] int? accountId = null,
        [FromQuery] string? search = null)
    {
        var result = await _service.GetPagedAsync(page, pageSize, accountId, search);

        return Ok(ApiResponse<object>.Ok(result, "Success", 200));
    }

    /// <summary>
    /// Bulk upload devices
    /// </summary>
    [HttpPost("bulk")]
    public async Task<IActionResult> BulkUpload([FromBody] List<CreateDeviceDto> devices)
    {
        var result = await _service.BulkCreateAsync(devices);

        return Ok(ApiResponse<object>.Ok(
            result,
            "Devices created successfully",
            200));
    }
}