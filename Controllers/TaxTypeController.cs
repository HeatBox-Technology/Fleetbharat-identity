using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/tax-types")]
public class TaxTypeController : ControllerBase
{
    private readonly ITaxTypeService _service;

    public TaxTypeController(ITaxTypeService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateTaxTypeRequest req)
    {
        var id = await _service.CreateAsync(req);
        return Ok(ApiResponse<object>.Ok(new { taxTypeId = id }, "TaxType created", 200));
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] int? countryId, [FromQuery] bool? isActive)
    {
        var data = await _service.GetAllAsync(search, countryId, isActive);
        return Ok(ApiResponse<object>.Ok(data, "Success", 200));
    }

    [HttpGet("{taxTypeId}")]
    public async Task<IActionResult> GetById(int taxTypeId)
    {
        var data = await _service.GetByIdAsync(taxTypeId);

        if (data == null)
            return NotFound(ApiResponse<object>.Fail("TaxType not found", 404));

        return Ok(ApiResponse<object>.Ok(data, "Success", 200));
    }

    [HttpGet("by-country/{countryId}")]
    public async Task<IActionResult> GetByCountry(int countryId)
    {
        var data = await _service.GetByCountryAsync(countryId);
        return Ok(ApiResponse<object>.Ok(data, "Success", 200));
    }

    [HttpPut("{taxTypeId}")]
    public async Task<IActionResult> Update(int taxTypeId, UpdateTaxTypeRequest req)
    {
        var ok = await _service.UpdateAsync(taxTypeId, req);
        if (!ok)
            return NotFound(ApiResponse<object>.Fail("TaxType not found", 404));

        return Ok(ApiResponse<string>.Ok("Updated", "TaxType updated", 200));
    }

    [HttpPatch("{taxTypeId}/status")]
    public async Task<IActionResult> UpdateStatus(int taxTypeId, [FromQuery] bool isActive)
    {
        var ok = await _service.UpdateStatusAsync(taxTypeId, isActive);
        if (!ok)
            return NotFound(ApiResponse<object>.Fail("TaxType not found", 404));

        return Ok(ApiResponse<string>.Ok("Updated", "Status updated", 200));
    }

    [HttpDelete("{taxTypeId}")]
    public async Task<IActionResult> Delete(int taxTypeId)
    {
        var ok = await _service.DeleteAsync(taxTypeId);
        if (!ok)
            return NotFound(ApiResponse<object>.Fail("TaxType not found", 404));

        return Ok(ApiResponse<string>.Ok("Deleted", "TaxType deleted", 200));
    }
}
