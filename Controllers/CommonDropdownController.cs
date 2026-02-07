using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/common/dropdowns")]
public class CommonDropdownController : ControllerBase
{
    private readonly ICommonDropdownService _service;

    public CommonDropdownController(ICommonDropdownService service)
    {
        _service = service;
    }

    [HttpGet("accounts")]
    public async Task<IActionResult> Accounts([FromQuery] string? search = null, [FromQuery] int limit = 20)
    {
        var data = await _service.GetAccountsAsync(search, limit);
        return Ok(ApiResponse<object>.Ok(data, "Success"));
    }

    [HttpGet("categories")]
    public async Task<IActionResult> Categories([FromQuery] string? search = null, [FromQuery] int limit = 20)
    {
        var data = await _service.GetCategoriesAsync(search, limit);
        return Ok(ApiResponse<object>.Ok(data, "Success"));
    }

    [HttpGet("roles")]
    public async Task<IActionResult> Roles(
        [FromQuery] int accountId,
        [FromQuery] string? search = null,
        [FromQuery] int limit = 20)
    {
        var data = await _service.GetRolesAsync(accountId, search, limit);
        return Ok(ApiResponse<object>.Ok(data, "Success"));
    }

    [HttpGet("users")]
    public async Task<IActionResult> Users(
        [FromQuery] int accountId,
        [FromQuery] string? search = null,
        [FromQuery] int limit = 20)
    {
        var data = await _service.GetUsersAsync(accountId, search, limit);
        return Ok(ApiResponse<object>.Ok(data, "Success"));
    }

    [HttpGet("Currency")]
    public async Task<IActionResult> GetCurrency()
    {
        var result = await _service.GetCurrencyDropdownAsync();
        return Ok(ApiResponse<object>.Ok(result, "Success"));
    }
    [HttpGet("form-modules/dropdown")]
    public async Task<IActionResult> GetFormModulesDropdown()
    {
        var data = await _service.GetFormModuleDropdownAsync();
        return Ok(ApiResponse<object>.Ok(data, "Success"));
    }


}
