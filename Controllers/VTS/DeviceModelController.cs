using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/devicemodel")]
[Authorize]
public class DeviceModelController : ControllerBase
{
    private readonly IDeviceModelService _service;

    public DeviceModelController(IDeviceModelService service)
    {
        _service = service;
    }

    /// <summary>
    /// Get device models with pagination, search, and filters.
    /// </summary>
    [HttpGet("getall")]
    public async Task<IActionResult> GetAll(
        [FromQuery] DeviceModelGetAllRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await _service.GetAllAsync(request, cancellationToken);
        return Ok(ApiResponse<object>.Ok(result, "Success", 200));
    }

    /// <summary>
    /// Get device model by id.
    /// </summary>
    [HttpGet("getbyid/{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _service.GetByIdAsync(id, cancellationToken);

        if (result == null)
            return NotFound(ApiResponse<object>.Fail("Device model not found", 404));

        return Ok(ApiResponse<object>.Ok(result, "Success", 200));
    }

    /// <summary>
    /// Create a new device model.
    /// </summary>
    [HttpPost("add")]
    public async Task<IActionResult> Add(
        [FromBody] CreateDeviceModelRequestDto request,
        CancellationToken cancellationToken)
    {
        var id = await _service.AddAsync(request, cancellationToken);

        return Ok(ApiResponse<object>.Ok(
            new { id },
            "Device model created successfully",
            200));
    }

    /// <summary>
    /// Update an existing device model.
    /// </summary>
    [HttpPut("update")]
    public async Task<IActionResult> Update(
        [FromBody] UpdateDeviceModelRequestDto request,
        CancellationToken cancellationToken)
    {
        var updated = await _service.UpdateAsync(request, cancellationToken);

        if (!updated)
            return NotFound(ApiResponse<object>.Fail("Device model not found", 404));

        return Ok(ApiResponse<string>.Ok("Updated", "Device model updated successfully", 200));
    }

    /// <summary>
    /// Soft delete a device model.
    /// </summary>
    [HttpDelete("delete/{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await _service.DeleteAsync(id, cancellationToken);

        if (!deleted)
            return NotFound(ApiResponse<object>.Fail("Device model not found", 404));

        return Ok(ApiResponse<string>.Ok("Deleted", "Device model deleted successfully", 200));
    }

    /// <summary>
    /// Get manufacturer dropdown values.
    /// </summary>
    [HttpGet("manufacturer-dropdown")]
    public async Task<IActionResult> GetManufacturerDropdown(CancellationToken cancellationToken)
    {
        var result = await _service.GetManufacturerDropdownAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(result, "Success", 200));
    }

    /// <summary>
    /// Get device type dropdown values.
    /// </summary>
    [HttpGet("device-type-dropdown")]
    public async Task<IActionResult> GetDeviceTypeDropdown(CancellationToken cancellationToken)
    {
        var result = await _service.GetDeviceTypeDropdownAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(result, "Success", 200));
    }

    [ApiExplorerSettings(IgnoreApi = true)]
    [HttpGet("/api/device-models/list")]
    public Task<IActionResult> LegacyGetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        return GetAll(
            new DeviceModelGetAllRequestDto
            {
                Page = page,
                PageSize = pageSize,
                Search = search
            },
            cancellationToken);
    }

    [ApiExplorerSettings(IgnoreApi = true)]
    [HttpGet("/api/device-models/{id:int}")]
    public Task<IActionResult> LegacyGetById(int id, CancellationToken cancellationToken)
    {
        return GetById(id, cancellationToken);
    }

    [ApiExplorerSettings(IgnoreApi = true)]
    [HttpPost("/api/device-models")]
    public Task<IActionResult> LegacyAdd(
        [FromBody] LegacyCreateDeviceModelRequestDto request,
        CancellationToken cancellationToken)
    {
        return Add(
            new CreateDeviceModelRequestDto
            {
                ManufacturerId = request.ManufacturerId,
                Name = request.DisplayName,
                DeviceCategoryId = request.DeviceCategoryId,
                UseIMEIAsPrimaryId = false,
                DeviceNo = null,
                IMEISerialNumber = null,
                IsEnabled = request.IsEnabled
            },
            cancellationToken);
    }

    [ApiExplorerSettings(IgnoreApi = true)]
    [HttpPut("/api/device-models/{id:int}")]
    public async Task<IActionResult> LegacyUpdate(
        int id,
        [FromBody] LegacyUpdateDeviceModelRequestDto request,
        CancellationToken cancellationToken)
    {
        var existing = await _service.GetByIdAsync(id, cancellationToken);
        if (existing == null)
            return NotFound(ApiResponse<object>.Fail("Device model not found", 404));

        return await Update(
            new UpdateDeviceModelRequestDto
            {
                Id = id,
                ManufacturerId = request.ManufacturerId,
                Name = request.DisplayName,
                DeviceCategoryId = request.DeviceCategoryId,
                UseIMEIAsPrimaryId = existing.UseIMEIAsPrimaryId,
                DeviceNo = existing.DeviceNo,
                IMEISerialNumber = existing.IMEISerialNumber,
                IsEnabled = request.IsEnabled
            },
            cancellationToken);
    }

    [ApiExplorerSettings(IgnoreApi = true)]
    [HttpPatch("/api/device-models/{id:int}/status")]
    public async Task<IActionResult> LegacyUpdateStatus(
        int id,
        [FromQuery] bool isEnabled,
        CancellationToken cancellationToken)
    {
        var existing = await _service.GetByIdAsync(id, cancellationToken);
        if (existing == null)
            return NotFound(ApiResponse<object>.Fail("Device model not found", 404));

        return await Update(
            new UpdateDeviceModelRequestDto
            {
                Id = id,
                ManufacturerId = existing.ManufacturerId,
                Name = existing.Name,
                DeviceCategoryId = existing.DeviceCategoryId,
                UseIMEIAsPrimaryId = existing.UseIMEIAsPrimaryId,
                DeviceNo = existing.DeviceNo,
                IMEISerialNumber = existing.IMEISerialNumber,
                IsEnabled = isEnabled
            },
            cancellationToken);
    }

    [ApiExplorerSettings(IgnoreApi = true)]
    [HttpDelete("/api/device-models/{id:int}")]
    public Task<IActionResult> LegacyDelete(int id, CancellationToken cancellationToken)
    {
        return Delete(id, cancellationToken);
    }
}

public class LegacyCreateDeviceModelRequestDto
{
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ManufacturerId { get; set; }
    public int DeviceCategoryId { get; set; }
    public string ProtocolType { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
}

public class LegacyUpdateDeviceModelRequestDto : LegacyCreateDeviceModelRequestDto
{
}
