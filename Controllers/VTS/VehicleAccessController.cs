using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/vehicle-access")]
public class VehicleAccessController : ControllerBase
{
    private readonly IVehicleAccessService _service;

    public VehicleAccessController(IVehicleAccessService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateVehicleAccessRequest request)
    {
        try
        {
            var id = await _service.CreateAsync(request);
            return Ok(ApiResponse<object>.Ok(new { id }, "Vehicle access created", 200));
        }
        catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
        }
    }

    [HttpPut("{id:long}")]
    public async Task<IActionResult> Update(long id, [FromBody] UpdateVehicleAccessRequest request)
    {
        try
        {
            var ok = await _service.UpdateAsync(id, request);
            if (!ok)
                return NotFound(ApiResponse<object>.Fail("Vehicle access not found", 404));

            return Ok(ApiResponse<string>.Ok("Updated", "Vehicle access updated", 200));
        }
        catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? accountId = null,
        [FromQuery] string? search = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var data = await _service.GetAllAsync(accountId, search, pageNumber, pageSize);
        return Ok(ApiResponse<object>.Ok(data, "Success", 200));
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetById(long id)
    {
        var data = await _service.GetByIdAsync(id);
        if (data == null)
            return NotFound(ApiResponse<object>.Fail("Vehicle access not found", 404));

        return Ok(ApiResponse<object>.Ok(data, "Success", 200));
    }

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, [FromBody] DeleteVehicleAccessRequest request)
    {
        try
        {
            var ok = await _service.DeleteAsync(id, request);
            if (!ok)
                return NotFound(ApiResponse<object>.Fail("Vehicle access not found", 404));

            return Ok(ApiResponse<string>.Ok("Deleted", "Vehicle access deleted", 200));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
        }
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export(
        [FromQuery] int? accountId = null,
        [FromQuery] string? search = null)
    {
        var fileBytes = await _service.ExportCsvAsync(accountId, search);
        return File(
            fileBytes,
            "text/csv",
            $"vehicle_access_{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
    }
}
