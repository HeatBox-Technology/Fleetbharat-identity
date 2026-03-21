using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

[ApiController]
[Route("api/geofences")]
// Remove this in production, added for testing
public class GeofenceController : ControllerBase
{
    private readonly IGeofenceService _service;

    public GeofenceController(IGeofenceService service)
    {
        _service = service;
    }

    /// <summary>
    /// Create geofence
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateGeofenceDto req)
    {
        var id = await _service.CreateAsync(req);

        return Ok(ApiResponse<object>.Ok(
            new { geofenceId = id },
            "Geofence created",
            200));
    }

    /// <summary>
    /// Get geofences with summary + pagination
    /// </summary>
    [HttpGet("list")]
    public async Task<IActionResult> GetZones(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] int? accountId = null,
        [FromQuery] string? search = null)
    {
        var result = await _service.GetZones(page, pageSize, accountId, search);

        return Ok(ApiResponse<object>.Ok(result, "Success", 200));
    }

    /// <summary>
    /// Get paged only
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
    /// Get geofence by id
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var data = await _service.GetByIdAsync(id);

        if (data == null)
            return NotFound(ApiResponse<object>.Fail("Geofence not found", 404));

        return Ok(ApiResponse<object>.Ok(data, "Success", 200));
    }

    /// <summary>
    /// Update geofence
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateGeofenceDto req)
    {
        var ok = await _service.UpdateAsync(id, req);

        if (!ok)
            return NotFound(ApiResponse<object>.Fail("Geofence not found", 404));

        return Ok(ApiResponse<string>.Ok("Updated", "Geofence updated", 200));
    }

    /// <summary>
    /// Enable / Disable geofence
    /// </summary>
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(
        int id,
        [FromQuery] bool isEnabled)
    {
        var ok = await _service.UpdateStatusAsync(id, isEnabled);

        if (!ok)
            return NotFound(ApiResponse<object>.Fail("Geofence not found", 404));

        return Ok(ApiResponse<string>.Ok("Updated", "Status updated", 200));
    }

    /// <summary>
    /// Soft delete geofence
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var ok = await _service.DeleteAsync(id);

        if (!ok)
            return NotFound(ApiResponse<object>.Fail("Geofence not found", 404));

        return Ok(ApiResponse<string>.Ok("Deleted", "Geofence deleted", 200));
    }
    /// <summary>
    /// Bulk create geofences
    /// </summary>
    [HttpPost("bulk")]
    public async Task<IActionResult> BulkUpload([FromBody] List<CreateGeofenceDto> items)
    {
        var result = await _service.BulkCreateAsync(items);

        return Ok(ApiResponse<object>.Ok(
            result,
            "Geofences created successfully",
            200));
    }
}