using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/cities")]
public class CityController : ControllerBase
{
    private readonly ICityService _service;

    public CityController(ICityService service)
    {
        _service = service;
    }

    // POST – Add
    [HttpPost]
    public async Task<IActionResult> Create(mst_city city)
    {
        var result = await _service.CreateAsync(city);
        return Ok(result);
    }

    // GET – Fetch all
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await _service.GetAllAsync());
    }

    // GET – Fetch by state
    [HttpGet("by-state/{stateId}")]
    public async Task<IActionResult> GetByState(int stateId)
    {
        return Ok(await _service.GetByStateAsync(stateId));
    }

    // GET – Fetch by id
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var city = await _service.GetByIdAsync(id);
        if (city == null) return NotFound();
        return Ok(city);
    }

    // PUT – Full update
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, mst_city city)
    {
        await _service.UpdateAsync(id, city);
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
