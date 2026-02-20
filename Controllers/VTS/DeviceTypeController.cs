using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/device-types")]
public class DeviceTypeController : ControllerBase
{
    private readonly IDeviceTypeService _service;

    public DeviceTypeController(IDeviceTypeService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create(DeviceTypeDto req)
    {
        var id = await _service.CreateAsync(req);

        return Ok(ApiResponse<object>.Ok(
            new { deviceTypeId = id },
            "Device type created",
            200));
    }

    [HttpGet("list")]
    public async Task<IActionResult> GetDeviceTypes(
        int page = 1,
        int pageSize = 10,
        string? search = null)
    {
        var result = await _service.GetDeviceTypes(page, pageSize, search);

        return Ok(ApiResponse<object>.Ok(result, "Success", 200));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var data = await _service.GetByIdAsync(id);

        if (data == null)
            return NotFound(ApiResponse<object>.Fail("Not found", 404));

        return Ok(ApiResponse<object>.Ok(data, "Success", 200));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, DeviceTypeDto req)
    {
        var ok = await _service.UpdateAsync(id, req);

        if (!ok)
            return NotFound(ApiResponse<object>.Fail("Not found", 404));

        return Ok(ApiResponse<string>.Ok("Updated", "Device type updated", 200));
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, bool isEnabled)
    {
        var ok = await _service.UpdateStatusAsync(id, isEnabled);

        if (!ok)
            return NotFound(ApiResponse<object>.Fail("Not found", 404));

        return Ok(ApiResponse<string>.Ok("Updated", "Status updated", 200));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var ok = await _service.DeleteAsync(id);

        if (!ok)
            return NotFound(ApiResponse<object>.Fail("Not found", 404));

        return Ok(ApiResponse<string>.Ok("Deleted", "Device type deleted", 200));
    }
}