using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/categories")]
public class CategoryController : ControllerBase
{
    private readonly ICategoryService _service;

    public CategoryController(ICategoryService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateCategoryRequest req)
    {
        var id = await _service.CreateAsync(req);
        return Ok(ApiResponse<object>.Ok(new { categoryId = id }, "Category created", 200));
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search = null, [FromQuery] bool? isActive = null)
    {
        var list = await _service.GetAllAsync(search, isActive);
        return Ok(ApiResponse<object>.Ok(list, "Success", 200));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var data = await _service.GetByIdAsync(id);
        if (data == null)
            return NotFound(ApiResponse<object>.Fail("Category not found", 404));

        return Ok(ApiResponse<object>.Ok(data, "Success", 200));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UpdateCategoryRequest req)
    {
        var ok = await _service.UpdateAsync(id, req);
        if (!ok)
            return NotFound(ApiResponse<object>.Fail("Category not found", 404));

        return Ok(ApiResponse<string>.Ok("Updated", "Category updated", 200));
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromQuery] bool isActive)
    {
        var ok = await _service.UpdateStatusAsync(id, isActive);
        if (!ok)
            return NotFound(ApiResponse<object>.Fail("Category not found", 404));

        return Ok(ApiResponse<string>.Ok("Updated", "Status updated", 200));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var ok = await _service.DeleteAsync(id);
        if (!ok)
            return NotFound(ApiResponse<object>.Fail("Category not found", 404));

        return Ok(ApiResponse<string>.Ok("Deleted", "Category deleted", 200));
    }
}
