using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/forms")]
public class FormsController : ControllerBase
{
    private readonly IFormService _service;

    public FormsController(IFormService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateFormRequest req)
    {
        var id = await _service.CreateAsync(req);
        return Ok(ApiResponse<object>.Ok(new { formId = id }, "Form created", 200));
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] bool? isActive = null)
    {
        var data = await _service.GetAllAsync(page, pageSize, search, isActive);
        return Ok(ApiResponse<object>.Ok(data, "Success", 200));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var data = await _service.GetByIdAsync(id);
        if (data == null) return NotFound(ApiResponse<object>.Fail("Form not found", 404));
        return Ok(ApiResponse<object>.Ok(data, "Success", 200));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UpdateFormRequest req)
    {
        var ok = await _service.UpdateAsync(id, req);
        if (!ok) return NotFound(ApiResponse<object>.Fail("Form not found", 404));
        return Ok(ApiResponse<string>.Ok("Updated", "Form updated", 200));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var ok = await _service.DeleteAsync(id);
        if (!ok) return NotFound(ApiResponse<object>.Fail("Form not found", 404));
        return Ok(ApiResponse<string>.Ok("Deleted", "Form deleted", 200));
    }
}
