using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/forms")]
public class FormController : ControllerBase
{
    private readonly IFormService _service;

    public FormController(IFormService service)
    {
        _service = service;
    }

    // POST – Add
    [HttpPost]
    public async Task<IActionResult> Create(mst_form form)
    {
        var result = await _service.CreateAsync(form);
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
        var form = await _service.GetByIdAsync(id);
        if (form == null) return NotFound();
        return Ok(form);
    }

    // PUT – Full update
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, mst_form form)
    {
        await _service.UpdateAsync(id, form);
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
