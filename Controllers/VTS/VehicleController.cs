using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// API controller for managing vehicles.
/// </summary>
[ApiController]
[Route("api/vehicles")]
[AllowAnonymous]
//[Authorize] // Enable if authentication required
public class VehicleController : ControllerBase
{
    private readonly IVehicleService _service;

    public VehicleController(IVehicleService service)
    {
        _service = service;
    }

    /// <summary>
    /// Create vehicle
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateVehicleDto req)
    {
        var id = await _service.CreateAsync(req);

        return Ok(ApiResponse<object>.Ok(
            new { vehicleId = id },
            "Vehicle created",
            200));
    }

    /// <summary>
    /// Get vehicles with summary + pagination
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null)
    {
        var result = await _service.GetVehicles(page, pageSize, search);

        return Ok(ApiResponse<object>.Ok(
            result,
            "Success",
            200));
    }

    /// <summary>
    /// Get vehicle by id
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var data = await _service.GetByIdAsync(id);

        if (data == null)
            return NotFound(ApiResponse<object>.Fail("Vehicle not found", 404));

        return Ok(ApiResponse<object>.Ok(
            data,
            "Success",
            200));
    }

    /// <summary>
    /// Update vehicle
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateVehicleDto req)
    {
        var ok = await _service.UpdateAsync(id, req);

        if (!ok)
            return NotFound(ApiResponse<object>.Fail("Vehicle not found", 404));

        return Ok(ApiResponse<string>.Ok(
            "Updated",
            "Vehicle updated",
            200));
    }

    /// <summary>
    /// Update vehicle status
    /// </summary>
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromQuery] string status)
    {
        var ok = await _service.UpdateStatusAsync(id, status);

        if (!ok)
            return NotFound(ApiResponse<object>.Fail("Vehicle not found", 404));

        return Ok(ApiResponse<string>.Ok(
            "Updated",
            "Status updated",
            200));
    }

    /// <summary>
    /// Delete vehicle (Soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var ok = await _service.DeleteAsync(id);

        if (!ok)
            return NotFound(ApiResponse<object>.Fail("Vehicle not found", 404));

        return Ok(ApiResponse<string>.Ok(
            "Deleted",
            "Vehicle deleted",
            200));
    }

    /// <summary>
    /// Get paginated vehicles
    /// </summary>
    [HttpGet("paged")]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null)
    {
        var result = await _service.GetPagedAsync(page, pageSize, search);

        return Ok(ApiResponse<object>.Ok(
            result,
            "Success",
            200));
    }

    /// <summary>
    /// Bulk upload vehicles
    /// </summary>
    [HttpPost("bulk")]
    public async Task<IActionResult> BulkUpload([FromBody] List<CreateVehicleDto> vehicles)
    {
        var result = await _service.BulkCreateAsync(vehicles);

        return Ok(ApiResponse<object>.Ok(
            result,
            "Vehicles created successfully",
            200));
    }
}