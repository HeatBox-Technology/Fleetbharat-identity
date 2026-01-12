using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace FleetBharat.AccountService.Controller;

[ApiController]
[Route("api/states")]
public class StateController : ControllerBase
{
    private readonly IStateService _service;

    public StateController(IStateService service)
    {
        _service = service;
    }

    // POST – Add
    [HttpPost]
    public async Task<IActionResult> Create(mst_state state)
    {
        var result = await _service.CreateAsync(state);
        return Ok(result);
    }

    // GET – Fetch all
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await _service.GetAllAsync());
    }

    // GET – Fetch by country
    [HttpGet("by-country/{countryId}")]
    public async Task<IActionResult> GetByCountry(int countryId)
    {
        return Ok(await _service.GetByCountryAsync(countryId));
    }

    // GET – Fetch by id
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var state = await _service.GetByIdAsync(id);
        if (state == null) return NotFound();
        return Ok(state);
    }

    // PUT – Full update
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, mst_state state)
    {
        await _service.UpdateAsync(id, state);
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
