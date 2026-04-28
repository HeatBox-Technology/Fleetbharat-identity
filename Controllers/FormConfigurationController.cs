using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api")]
public class FormConfigurationController : ControllerBase
{
    private readonly IFormConfigurationService _service;

    public FormConfigurationController(IFormConfigurationService service)
    {
        _service = service;
    }

    [HttpGet("form-pages")]
    public async Task<IActionResult> GetFormPages()
    {
        var data = await _service.GetFormPagesAsync();
        return Ok(ApiResponse<object>.Ok(data, "Success", 200));
    }

    [HttpGet("form-fields")]
    public async Task<IActionResult> GetFormFields([FromQuery] string pageKey)
    {
        var data = await _service.GetFormFieldsAsync(pageKey);
        return Ok(ApiResponse<object>.Ok(data, "Success", 200));
    }

    [HttpPost("form-fields")]
    public async Task<IActionResult> CreateFormField([FromBody] CreateFormFieldRequestDto request)
    {
        var data = await _service.CreateFormFieldAsync(request);
        return Ok(ApiResponse<object>.Ok(data, "Field created successfully", 200));
    }

    [HttpGet("form-config")]
    public async Task<IActionResult> GetFormConfiguration(
        [FromQuery] int accountId,
        [FromQuery] string pageKey)
    {
        var data = await _service.GetFormConfigurationAsync(accountId, pageKey);
        return Ok(ApiResponse<object>.Ok(data, "Success", 200));
    }

    [HttpPost("form-config")]
    public async Task<IActionResult> SaveFormConfiguration([FromBody] SaveFormConfigurationRequestDto request)
    {
        await _service.SaveFormConfigurationAsync(request);
        return Ok(ApiResponse<string>.Ok("Saved", "Form configuration saved successfully", 200));
    }
}
