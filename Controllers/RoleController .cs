using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/roles")]
public class RoleController : ControllerBase
{
    private readonly IRoleService _service;

    public RoleController(IRoleService service)
    {
        _service = service;
    }

    // POST – Add
    [HttpPost]
    public async Task<IActionResult> Create(mst_role role)
    {
        var result = await _service.CreateAsync(role);
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
        var role = await _service.GetByIdAsync(id);
        if (role == null) return NotFound();
        return Ok(role);
    }

    // PUT – Full update
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, mst_role role)
    {
        await _service.UpdateAsync(id, role);
        return NoContent();
    }

    // PATCH – Update status only
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
