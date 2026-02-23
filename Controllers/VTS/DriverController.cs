using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs;
using Microsoft.AspNetCore.Mvc;


/// <summary>
/// API endpoints for managing drivers.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DriverController : ControllerBase
{
    private readonly IDriverService _service;
    public DriverController(IDriverService service)
    {
        _service = service;
    }

    /// <summary>
    /// Bulk upload drivers.
    /// </summary>
    /// <remarks>Uploads multiple drivers at once.</remarks>
    /// <param name="drivers">List of drivers to upload.</param>
    [HttpPost("bulk-upload")]
    public async Task<IActionResult> BulkUpload([FromBody] IEnumerable<DriverDto> drivers)
    {
        var result = await _service.BulkCreateAsync(drivers);
        return Ok(result);
    }

    /// <summary>
    /// Gets a paged list of drivers.
    /// </summary>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Page size.</param>
    /// <returns>Paged result of drivers.</returns>
    /// <remarks>
    /// Sample response:
    /// {
    ///   "data": [ { /* DriverDto fields */ } ],
    ///   "totalCount": 100,
    ///   "page": 1,
    ///   "pageSize": 10
    /// }
    /// </remarks>
    [HttpGet]
    public async Task<IActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _service.GetPagedAsync(page, pageSize);
        return Ok(result);
    }
}

