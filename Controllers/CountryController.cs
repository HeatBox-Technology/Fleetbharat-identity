using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/countries")]
public class CountryController : ControllerBase
{
    private readonly ICountryService _service;

    public CountryController(ICountryService service)
    {
        _service = service;
    }

    // POST – Add
    [HttpPost]
    public async Task<IActionResult> Create(mst_country country)
    {
        var result = await _service.CreateAsync(country);
        return Ok(result);
    }

    // GET – Fetch all
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await _service.GetAllAsync());
    }

    // GET – Fetch by id
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var country = await _service.GetByIdAsync(id);
        if (country == null) return NotFound();
        return Ok(country);
    }

    // PUT – Full update
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, mst_country country)
    {
        await _service.UpdateAsync(id, country);
        return NoContent();
    }

    // PATCH – Update active status
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromQuery] bool isActive)
    {
        await _service.UpdateStatusAsync(id, isActive);
        return NoContent();
    }

    // DELETE
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}
