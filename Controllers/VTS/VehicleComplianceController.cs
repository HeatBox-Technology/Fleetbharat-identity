using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/vehicle-compliance")]
public class VehicleComplianceController : ControllerBase
{
    private readonly IVehicleComplianceService _service;

    public VehicleComplianceController(IVehicleComplianceService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateVehicleComplianceDto dto)
    {
        var id = await _service.CreateAsync(dto);
        return Ok(ApiResponse<object>.Ok(new { id }, "Compliance created", 200));
    }

    [HttpPost("form")]
    public async Task<IActionResult> CreateForm([FromForm] VehicleComplianceFormDto dto)
    {
        var id = await _service.CreateAsync(dto, dto.Document);
        return Ok(ApiResponse<object>.Ok(new { id }, "Compliance created", 200));
    }

    [HttpGet("list")]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] int? accountId = null,
        [FromQuery] int? vehicleId = null,
        [FromQuery] string? complianceType = null,
        [FromQuery] string? status = null,
        [FromQuery] string? search = null)
    {
        var result = await _service.GetDocuments(
            page,
            pageSize,
            accountId,
            vehicleId,
            complianceType,
            status,
            search);

        return Ok(ApiResponse<object>.Ok(result, "Success", 200));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var data = await _service.GetByIdAsync(id);

        if (data == null)
            return NotFound(ApiResponse<object>.Fail("Compliance not found", 404));

        return Ok(ApiResponse<object>.Ok(data, "Success", 200));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateVehicleComplianceDto dto)
    {
        var ok = await _service.UpdateAsync(id, dto);

        if (!ok)
            return NotFound(ApiResponse<object>.Fail("Compliance not found", 404));

        return Ok(ApiResponse<string>.Ok("Updated", "Compliance updated", 200));
    }

    [HttpPut("{id}/form")]
    public async Task<IActionResult> UpdateForm(int id, [FromForm] UpdateVehicleComplianceFormDto dto)
    {
        var ok = await _service.UpdateAsync(id, dto, dto.Document);

        if (!ok)
            return NotFound(ApiResponse<object>.Fail("Compliance not found", 404));

        return Ok(ApiResponse<string>.Ok("Updated", "Compliance updated", 200));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var ok = await _service.DeleteAsync(id);

        if (!ok)
            return NotFound(ApiResponse<object>.Fail("Compliance not found", 404));

        return Ok(ApiResponse<string>.Ok("Deleted", "Compliance deleted", 200));
    }
}
