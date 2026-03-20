using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;


[ApiController]
[Route("api/form-role-rights")]
public class FormRoleRightController : ControllerBase
{
    private readonly IFormRoleRightService _service;

    public FormRoleRightController(IFormRoleRightService service)
    {
        _service = service;
    }

    // POST – Add
    [HttpPost]
    public async Task<IActionResult> Create(map_FormRole_right right)
    {
        var result = await _service.CreateAsync(right);
        return Ok(result);
    }

    // GET – Fetch all
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        return Ok(await _service.GetAllAsync());
    }

    // GET – Fetch by role
    [HttpGet("by-role/{roleId}")]
    public async Task<IActionResult> GetByRole(int roleId)
    {
        return Ok(await _service.GetByRoleAsync(roleId));
    }

    // GET – Fetch by id
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var right = await _service.GetByIdAsync(id);
        if (right == null) return NotFound();
        return Ok(right);
    }

    // PUT – Full update
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, map_FormRole_right right)
    {
        await _service.UpdateAsync(id, right);
        return NoContent();
    }

    // PATCH – Update permissions only
    [HttpPatch("{id}/rights")]
    public async Task<IActionResult> UpdateRights(int id, map_FormRole_right right)
    {
        await _service.UpdateRightsAsync(id, right);
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
