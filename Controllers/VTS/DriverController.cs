using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/drivers")]
public class DriverController : ControllerBase
{
    private readonly IDriverService _service;

    public DriverController(IDriverService service)
    {
        _service = service;
    }

    /// <summary>
    /// Get drivers with summary + pagination
    /// </summary>
    [HttpGet("list")]
    public async Task<IActionResult> GetDrivers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] int? accountId = null,
        [FromQuery] string? search = null)
    {
        var result = await _service.GetDrivers(page, pageSize, accountId, search);

        return Ok(ApiResponse<object>.Ok(
            result,
            "Success",
            200));
    }

    /// <summary>
    /// Get driver by Id
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var data = await _service.GetByIdAsync(id);

        if (data == null)
            return NotFound(ApiResponse<object>.Fail("Driver not found", 404));

        return Ok(ApiResponse<object>.Ok(data, "Success", 200));
    }

    /// <summary>
    /// Create driver
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDriverDto dto)
    {
        var id = await _service.CreateAsync(dto);

        return Ok(ApiResponse<object>.Ok(
            new { driverId = id },
            "Driver created",
            200));
    }

    /// <summary>
    /// Update driver
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateDriverDto dto)
    {
        var ok = await _service.UpdateAsync(id, dto);

        if (!ok)
            return NotFound(ApiResponse<object>.Fail("Driver not found", 404));

        return Ok(ApiResponse<string>.Ok(
            "Updated",
            "Driver updated",
            200));
    }

    /// <summary>
    /// Update driver status
    /// </summary>
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(
        int id,
        [FromQuery] bool isActive)
    {
        var ok = await _service.UpdateStatusAsync(id, isActive);

        if (!ok)
            return NotFound(ApiResponse<object>.Fail("Driver not found", 404));

        return Ok(ApiResponse<string>.Ok(
            "Updated",
            "Status updated",
            200));
    }

    /// <summary>
    /// Delete driver (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var ok = await _service.DeleteAsync(id);

        if (!ok)
            return NotFound(ApiResponse<object>.Fail("Driver not found", 404));

        return Ok(ApiResponse<string>.Ok(
            "Deleted",
            "Driver deleted",
            200));
    }

    /// <summary>
    /// Bulk upload drivers
    /// </summary>
    [HttpPost("bulk")]
    public async Task<IActionResult> BulkUpload([FromBody] List<CreateDriverDto> drivers)
    {
        var result = await _service.BulkCreateAsync(drivers);

        return Ok(ApiResponse<object>.Ok(
            result,
            "Drivers created successfully",
            200));
    }
    [HttpGet("export")]
    public async Task<IActionResult> ExportDrivers(
    [FromQuery] int? accountId,
    [FromQuery] string? search,
    [FromQuery] string format = "csv")
    {
        byte[] fileBytes;
        string contentType;
        string fileExtension;

        // Normalize format to lowercase
        format = format?.ToLower() ?? "csv";

        // Validate format parameter
        if (format != "csv" && format != "xlsx" && format != "excel")
        {
            return BadRequest(ApiResponse<object>.Fail(
                "Invalid format. Supported formats are 'csv' or 'excel'.",
                400));
        }

        if (format == "xlsx" || format == "excel")
        {
            fileBytes = await _service.ExportDriversXlsxAsync(accountId, search);
            contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            fileExtension = "xlsx";
        }
        else
        {
            fileBytes = await _service.ExportDriversCsvAsync(accountId, search);
            contentType = "text/csv";
            fileExtension = "csv";
        }

        return File(
            fileBytes,
            contentType,
            $"drivers_{System.DateTime.UtcNow:yyyyMMddHHmmss}.{fileExtension}"
        );
    }
}