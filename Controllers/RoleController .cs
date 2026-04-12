using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/roles")]
public class RolesController : ControllerBase
{
    private readonly IRoleService _service;

    public RolesController(IRoleService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateRoleRequest req)
    {
        var id = await _service.CreateAsync(req);
        return Ok(ApiResponse<object>.Ok(new { roleId = id }, "Role created", 200));
    }

    [HttpGet("by-account/{accountId}")]
    public async Task<IActionResult> GetByAccount(int accountId)
    {
        var list = await _service.GetByAccountAsync(accountId);
        return Ok(ApiResponse<object>.Ok(list, "Success", 200));
    }

    [HttpGet("{roleId}/rights")]
    public async Task<IActionResult> GetRights(int roleId)
    {
        var rights = await _service.GetRoleRightsAsync(roleId);
        return Ok(ApiResponse<object>.Ok(rights, "Success", 200));
    }

    [HttpPut("{roleId}")]
    public async Task<IActionResult> Update(int roleId, UpdateRoleRequest req)
    {
        var ok = await _service.UpdateAsync(roleId, req);
        if (!ok) return NotFound(ApiResponse<object>.Fail("Role not found", 404));
        return Ok(ApiResponse<string>.Ok("Updated", "Role updated", 200));
    }

    [HttpPut("{roleId}/rights")]
    public async Task<IActionResult> UpdateRights(int roleId, List<RoleFormRightDto> rights)
    {
        var ok = await _service.UpdateRightsAsync(roleId, rights);
        if (!ok) return NotFound(ApiResponse<object>.Fail("Role not found", 404));
        return Ok(ApiResponse<string>.Ok("Updated", "Role rights updated", 200));
    }

    [HttpDelete("{roleId}")]
    public async Task<IActionResult> Delete(int roleId)
    {
        var ok = await _service.DeleteAsync(roleId);
        if (!ok) return NotFound(ApiResponse<object>.Fail("Role not found", 404));
        return Ok(ApiResponse<string>.Ok("Deleted", "Role deleted", 200));
    }
    [HttpGet("GetAllRole")]
    public async Task<IActionResult> GetAllRole(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] int? accountId = null,
        [FromQuery] string? search = null)
    {
        var data = await _service.GetRoles(page, pageSize, accountId, search);
        return Ok(ApiResponse<object>.Ok(data, "Success", 200));
    }
    [HttpGet("{roleId}")]
    public async Task<IActionResult> GetByRoleId(
        int roleId,
        [FromQuery] int accountId)
    {
        var data = await _service.GetByRoleIdAsync(roleId, accountId);

        if (data == null)
            return NotFound(ApiResponse<object>.Fail("Role not found for this account", 404));

        return Ok(ApiResponse<object>.Ok(data, "Success", 200));
    }


    [HttpGet("export")]
    public async Task<IActionResult> ExportRoles(
        [FromQuery] int? accountId = null,
        [FromQuery] string? search = null,
        [FromQuery] string format = "csv")
    {
        format = format?.ToLower() ?? "csv";

        if (format != "csv" && format != "xlsx" && format != "excel")
        {
            return BadRequest(ApiResponse<object>.Fail(
                "Invalid format. Supported formats are 'csv', 'xlsx' or 'excel'.",
                400));
        }

        byte[] fileBytes;
        string contentType;
        string fileExtension;

        if (format == "xlsx" || format == "excel")
        {
            fileBytes = await _service.ExportRolesXlsxAsync(accountId, search);
            contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            fileExtension = "xlsx";
        }
        else
        {
            fileBytes = await _service.ExportRolesCsvAsync(accountId, search);
            contentType = "text/csv";
            fileExtension = "csv";
        }

        return File(
            fileBytes,
            contentType,
            $"roles_{DateTime.UtcNow:yyyyMMddHHmmss}.{fileExtension}");
    }


}
