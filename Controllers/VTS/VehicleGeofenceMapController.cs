using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// API controller for managing vehicle-geofence mappings.
/// </summary>
[ApiController]
[Route("api/vehicle-geofence")]
public class VehicleGeofenceMapController : ControllerBase
{
    private readonly IVehicleGeofenceMapService _service;

    public VehicleGeofenceMapController(IVehicleGeofenceMapService service)
    {
        _service = service;
    }

    /// <summary>
    /// Create mapping
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateVehicleGeofenceMapDto dto)
    {
        var id = await _service.CreateAsync(dto);

        return Ok(ApiResponse<object>.Ok(
            new { vehicleGeofenceMapId = id },
            "Mapping created successfully",
            200));
    }

    /// <summary>
    /// Get mappings with summary + pagination
    /// </summary>
    [HttpGet("list")]
    public async Task<IActionResult> GetAssignments(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] int? accountId = null,
        [FromQuery] string? search = null)
    {
        var result = await _service.GetAssignments(page, pageSize, accountId, search);

        return Ok(ApiResponse<object>.Ok(result, "Success", 200));
    }

    /// <summary>
    /// Get mapping by Id
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var data = await _service.GetByIdAsync(id);

        if (data == null)
            return NotFound(ApiResponse<object>.Fail("Mapping not found", 404));

        return Ok(ApiResponse<object>.Ok(data, "Success", 200));
    }

    /// <summary>
    /// Update mapping
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateVehicleGeofenceMapDto dto)
    {
        var ok = await _service.UpdateAsync(id, dto);

        if (!ok)
            return NotFound(ApiResponse<object>.Fail("Mapping not found", 404));

        return Ok(ApiResponse<string>.Ok("Updated", "Mapping updated successfully", 200));
    }

    /// <summary>
    /// Update active status
    /// </summary>
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(
        int id,
        [FromQuery] bool isActive)
    {
        var ok = await _service.UpdateStatusAsync(id, isActive);

        if (!ok)
            return NotFound(ApiResponse<object>.Fail("Mapping not found", 404));

        return Ok(ApiResponse<string>.Ok("Updated", "Status updated successfully", 200));
    }

    /// <summary>
    /// Delete mapping (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var ok = await _service.DeleteAsync(id);

        if (!ok)
            return NotFound(ApiResponse<object>.Fail("Mapping not found", 404));

        return Ok(ApiResponse<string>.Ok("Deleted", "Mapping deleted successfully", 200));
    }

    /// <summary>
    /// Get paged mappings (without summary)
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
}