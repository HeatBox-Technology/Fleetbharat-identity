using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/sims")]
public class SimController : ControllerBase
{
    private readonly ISimService _service;

    public SimController(ISimService service)
    {
        _service = service;
    }

    /// <summary>
    /// Get SIMs with summary + pagination
    /// </summary>
    [HttpGet("list")]
    public async Task<IActionResult> GetSims(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] int? accountId = null,
        [FromQuery] string? search = null)
    {
        var result = await _service.GetSims(page, pageSize, accountId, search);

        return Ok(ApiResponse<object>.Ok(
            result,
            "Success",
            200));
    }

    /// <summary>
    /// Get SIM by Id
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var data = await _service.GetByIdAsync(id);

        if (data == null)
            return NotFound(ApiResponse<object>.Fail("SIM not found", 404));

        return Ok(ApiResponse<object>.Ok(data, "Success", 200));
    }

    /// <summary>
    /// Create SIM
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSimDto dto)
    {
        var id = await _service.CreateAsync(dto);

        return Ok(ApiResponse<object>.Ok(
            new { simId = id },
            "SIM created",
            200));
    }

    /// <summary>
    /// Update SIM
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateSimDto dto)
    {
        var ok = await _service.UpdateAsync(id, dto);

        if (!ok)
            return NotFound(ApiResponse<object>.Fail("SIM not found", 404));

        return Ok(ApiResponse<string>.Ok(
            "Updated",
            "SIM updated",
            200));
    }

    /// <summary>
    /// Update SIM status
    /// </summary>
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(
        int id,
        [FromQuery] bool isActive)
    {
        var ok = await _service.UpdateStatusAsync(id, isActive);

        if (!ok)
            return NotFound(ApiResponse<object>.Fail("SIM not found", 404));

        return Ok(ApiResponse<string>.Ok(
            "Updated",
            "Status updated",
            200));
    }

    /// <summary>
    /// Delete SIM (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var ok = await _service.DeleteAsync(id);

        if (!ok)
            return NotFound(ApiResponse<object>.Fail("SIM not found", 404));

        return Ok(ApiResponse<string>.Ok(
            "Deleted",
            "SIM deleted",
            200));
    }

    /// <summary>
    /// Bulk upload SIMs
    /// </summary>
    [HttpPost("bulk")]
    public async Task<IActionResult> BulkUpload([FromBody] List<CreateSimDto> sims)
    {
        var result = await _service.BulkCreateAsync(sims);

        return Ok(ApiResponse<object>.Ok(
            result,
            "SIMs created successfully",
            200));
    }
}
