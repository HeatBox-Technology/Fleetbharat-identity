using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/accounts")]
public class AccountController : ControllerBase
{
    private readonly IAccountProvisionService _service;

    public AccountController(IAccountProvisionService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateAccountRequest req)
    {
        var id = await _service.CreateAsync(req);
        return Ok(ApiResponse<object>.Ok(new { accountId = id }, "Account created", 200));
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? page = null,
        [FromQuery] int? pageSize = null,
        [FromQuery] string? search = null,
        [FromQuery] bool? status = null)
    {
        var isListRequest =
            Request.Query.ContainsKey("page") ||
            Request.Query.ContainsKey("pageSize") ||
            Request.Query.ContainsKey("search") ||
            Request.Query.ContainsKey("status");

        if (!isListRequest)
        {
            var dropdown = await _service.GetDropdownAsync();
            return Ok(ApiResponse<object>.Ok(dropdown, "Success", 200));
        }

        var result = await _service.GetAllAsync(page ?? 1, pageSize ?? 10, search, status);
        return Ok(ApiResponse<object>.Ok(result, "Success", 200));
    }

    [HttpGet("list")]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] bool? status = null)
    {
        var result = await _service.GetAllAsync(page, pageSize, search, status);
        return Ok(ApiResponse<object>.Ok(result, "Success", 200));
    }

    [HttpGet("dropdown")]
    public async Task<IActionResult> GetDropdown()
    {
        var result = await _service.GetDropdownAsync();
        return Ok(ApiResponse<object>.Ok(result, "Success", 200));
    }

    [HttpGet("{accountId}")]
    public async Task<IActionResult> GetById(int accountId)
    {
        var result = await _service.GetByIdAsync(accountId);

        if (result == null)
            return NotFound(ApiResponse<object>.Fail("Account not found", 404));

        return Ok(ApiResponse<object>.Ok(result, "Success", 200));
    }

    [HttpGet("hierarchy")]
    public async Task<IActionResult> GetHierarchy()
    {
        var data = await _service.GetHierarchyAsync();
        return Ok(data);
    }

    [HttpPut("{accountId}")]
    public async Task<IActionResult> Update(int accountId, UpdateAccountRequest req)
    {
        var ok = await _service.UpdateAsync(accountId, req);

        if (!ok)
            return NotFound(ApiResponse<object>.Fail("Account not found", 404));

        return Ok(ApiResponse<string>.Ok("Updated", "Account updated", 200));
    }

    [HttpPatch("{accountId}/status")]
    public async Task<IActionResult> UpdateStatus(int accountId, [FromQuery] bool status)
    {
        var ok = await _service.UpdateStatusAsync(accountId, status);

        if (!ok)
            return NotFound(ApiResponse<object>.Fail("Account not found", 404));

        return Ok(ApiResponse<string>.Ok("Updated", "Status updated", 200));
    }

    [HttpDelete("{accountId}")]
    public async Task<IActionResult> Delete(int accountId)
    {
        var ok = await _service.DeleteAsync(accountId);

        if (!ok)
            return NotFound(ApiResponse<object>.Fail("Account not found", 404));

        return Ok(ApiResponse<string>.Ok("Deleted", "Account deleted", 200));
    }
}
