using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/account-configurations")]
public class AccountConfigurationController : ControllerBase
{
    private readonly IAccountConfigurationService _service;

    public AccountConfigurationController(IAccountConfigurationService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateAccountConfigurationRequest req)
    {
        var id = await _service.CreateAsync(req);
        return Ok(ApiResponse<object>.Ok(new { accountConfigurationId = id }, "Configuration created", 200));
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] int? accountId = null)
    {
        var data = await _service.GetAllAsync(page, pageSize, search, accountId);
        return Ok(ApiResponse<object>.Ok(data, "Success", 200));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var data = await _service.GetByIdAsync(id);
        if (data == null)
            return NotFound(ApiResponse<object>.Fail("Configuration not found", 404));

        return Ok(ApiResponse<object>.Ok(data, "Success", 200));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UpdateAccountConfigurationRequest req)
    {
        var ok = await _service.UpdateAsync(id, req);
        if (!ok)
            return NotFound(ApiResponse<object>.Fail("Configuration not found", 404));

        return Ok(ApiResponse<string>.Ok("Updated", "Configuration updated", 200));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var ok = await _service.DeleteAsync(id);
        if (!ok)
            return NotFound(ApiResponse<object>.Fail("Configuration not found", 404));

        return Ok(ApiResponse<string>.Ok("Deleted", "Configuration deleted", 200));
    }
}