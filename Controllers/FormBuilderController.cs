using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/form-builder")]
public class FormBuilderController : ControllerBase
{
    private readonly IFormBuilderService _service;

    public FormBuilderController(IFormBuilderService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateFormBuilderRequest request)
    {
        try
        {
            var id = await _service.CreateAsync(request);
            return Ok(ApiResponse<object>.Ok(
                new { id },
                "Form builder created successfully",
                200));
        }
        catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateFormBuilderRequest request)
    {
        try
        {
            var ok = await _service.UpdateAsync(id, request);
            if (!ok)
                return NotFound(ApiResponse<object>.Fail("Form builder not found", 404));

            return Ok(ApiResponse<string>.Ok(
                "Updated",
                "Form builder updated successfully",
                200));
        }
        catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? accountId = null,
        [FromQuery] int? fkFormId = null,
        [FromQuery] string? search = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var data = await _service.GetAllAsync(accountId, fkFormId, search, pageNumber, pageSize);
        return Ok(ApiResponse<object>.Ok(data, "Success", 200));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var data = await _service.GetByIdAsync(id);
        if (data == null)
            return NotFound(ApiResponse<object>.Fail("Form builder not found", 404));

        return Ok(ApiResponse<object>.Ok(data, "Success", 200));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, [FromBody] DeleteFormBuilderRequest request)
    {
        try
        {
            var ok = await _service.DeleteAsync(id, request);
            if (!ok)
                return NotFound(ApiResponse<object>.Fail("Form builder not found", 404));

            return Ok(ApiResponse<string>.Ok(
                "Deleted",
                "Form builder deleted successfully",
                200));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<object>.Fail(ex.Message, 400));
        }
    }

    [HttpGet("by-account-form")]
    public async Task<IActionResult> GetByAccountAndForm(
        [FromQuery] int accountId,
        [FromQuery] int fkFormId)
    {
        var data = await _service.GetByAccountAndFormAsync(accountId, fkFormId);
        if (data == null)
            return NotFound(ApiResponse<object>.Fail("Form builder not found", 404));

        return Ok(ApiResponse<object>.Ok(data, "Success", 200));
    }
}
