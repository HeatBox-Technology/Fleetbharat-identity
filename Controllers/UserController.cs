using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUserService _service;

    public UsersController(IUserService service)
    {
        _service = service;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CreateUser(
     [FromForm] CreateUserRequest req)
    {
        var userId = await _service.CreateAsync(req);
        return Ok(ApiResponse<object>.Ok(new { userId }, "User created"));
    }


    // ✅ 1) GET ALL USER LIST (UI)
    [HttpGet("GetAllUser")]
    public async Task<IActionResult> GetAllUser(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] int? accountId = null,
        [FromQuery] int? roleId = null,
        [FromQuery] bool? status = null,
        [FromQuery] bool? twoFactorEnabled = null,
        [FromQuery] string? search = null)
    {
        var data = await _service.GetUsersForUiAsync(page, pageSize, accountId, roleId, status, twoFactorEnabled, search);
        return Ok(ApiResponse<object>.Ok(data, "Success", 200));
    }

    // ✅ 2) GET USER BY ID
    [HttpGet("{GetuserById}")]
    public async Task<IActionResult> GetById(Guid userId)
    {
        var data = await _service.GetByIdAsync(userId);
        if (data == null)
            return NotFound(ApiResponse<object>.Fail("User not found", 404));

        return Ok(ApiResponse<object>.Ok(data, "Success", 200));
    }

    // ✅ 3) UPDATE USER
    [HttpPut("{UpdateUser}")]
    public async Task<IActionResult> Update(Guid userId, UpdateUserRequest req)
    {
        var updated = await _service.UpdateAsync(userId, req);

        if (!updated)
            return NotFound(ApiResponse<object>.Fail("User not found", 404));

        return Ok(ApiResponse<object>.Ok(new { userId }, "User updated successfully", 200));
    }

    // ✅ 4) DELETE USER (SOFT DELETE)
    [HttpDelete("{DeleteUser}")]
    public async Task<IActionResult> Delete(Guid userId)
    {
        var deleted = await _service.SoftDeleteAsync(userId);

        if (!deleted)
            return NotFound(ApiResponse<object>.Fail("User not found", 404));

        return Ok(ApiResponse<object>.Ok(new { userId }, "User deleted successfully", 200));
    }

}
