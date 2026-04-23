using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/device-models")]
public class DeviceModelController : ControllerBase
{
    private readonly IDeviceModelService _service;

    public DeviceModelController(IDeviceModelService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDeviceModelDto dto)
    {
        var id = await _service.CreateAsync(dto);
        return Ok(ApiResponse<object>.Ok(new { id }, "Device model created", 200));
    }

    [HttpGet("list")]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null)
    {
        var result = await _service.GetModels(page, pageSize, search);
        return Ok(ApiResponse<object>.Ok(result, "Success", 200));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var data = await _service.GetByIdAsync(id);

        if (data == null)
            return NotFound(ApiResponse<object>.Fail("Device model not found", 404));

        return Ok(ApiResponse<object>.Ok(data, "Success", 200));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateDeviceModelDto dto)
    {
        var ok = await _service.UpdateAsync(id, dto);

        if (!ok)
            return NotFound(ApiResponse<object>.Fail("Device model not found", 404));

        return Ok(ApiResponse<string>.Ok("Updated", "Device model updated", 200));
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromQuery] bool isEnabled)
    {
        var ok = await _service.UpdateStatusAsync(id, isEnabled);

        if (!ok)
            return NotFound(ApiResponse<object>.Fail("Device model not found", 404));

        return Ok(ApiResponse<string>.Ok("Updated", "Status updated", 200));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var ok = await _service.DeleteAsync(id);

        if (!ok)
            return NotFound(ApiResponse<object>.Fail("Device model not found", 404));

        return Ok(ApiResponse<string>.Ok("Deleted", "Device model deleted", 200));
    }
}
