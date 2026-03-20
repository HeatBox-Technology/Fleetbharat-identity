using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/oem-manufacturers")]
public class OemManufacturerController : ControllerBase
{
    private readonly IOemManufacturerService _service;

    public OemManufacturerController(IOemManufacturerService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create(OemManufacturerDto dto)
    {
        var id = await _service.CreateAsync(dto);

        return Ok(ApiResponse<object>.Ok(
            new { id },
            "Created",
            200));
    }

    [HttpGet("list")]
    public async Task<IActionResult> GetList(
        int page = 1,
        int pageSize = 10,
        string? search = null)
    {
        var result = await _service.GetManufacturers(page, pageSize, search);

        return Ok(ApiResponse<object>.Ok(result, "Success", 200));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var data = await _service.GetByIdAsync(id);

        if (data == null)
            return NotFound();

        return Ok(ApiResponse<object>.Ok(data, "Success", 200));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, OemManufacturerDto dto)
    {
        var ok = await _service.UpdateAsync(id, dto);

        if (!ok) return NotFound();

        return Ok(ApiResponse<string>.Ok("Updated", "Success", 200));
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, bool isEnabled)
    {
        var ok = await _service.UpdateStatusAsync(id, isEnabled);

        if (!ok) return NotFound();

        return Ok(ApiResponse<string>.Ok("Updated", "Success", 200));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var ok = await _service.DeleteAsync(id);

        if (!ok) return NotFound();

        return Ok(ApiResponse<string>.Ok("Deleted", "Success", 200));
    }
}