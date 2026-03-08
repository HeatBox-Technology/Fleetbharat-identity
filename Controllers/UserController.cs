using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
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


    // ----------------------------------------------------
    // PATCH : BASIC DETAILS
    // ----------------------------------------------------
    [HttpPatch("{id:guid}/basic")]
    public async Task<IActionResult> UpdateBasic(
        Guid id,
        [FromBody] UpdateUserBasicRequest request)
    {
        var ok = await _service.UpdateBasicAsync(id, request);

        if (!ok)
            return NotFound(ApiResponse<object>.Fail("User not found", 404));

        return Ok(ApiResponse<object>.Ok(null, "Basic details updated"));
    }

    // ----------------------------------------------------
    // PATCH : ROLE / ACCOUNT
    // ----------------------------------------------------
    [HttpPatch("{id:guid}/role")]
    public async Task<IActionResult> UpdateRole(
        Guid id,
        [FromBody] UpdateUserRoleRequest request)
    {
        var ok = await _service.UpdateRoleAsync(id, request);

        if (!ok)
            return NotFound(ApiResponse<object>.Fail("User not found", 404));

        return Ok(ApiResponse<object>.Ok(null, "Role updated"));
    }

    // ----------------------------------------------------
    // PATCH : USER LEVEL PERMISSIONS
    // ----------------------------------------------------
    [HttpPatch("{id:guid}/permissions")]
    public async Task<IActionResult> UpdatePermissions(
        Guid id,
        [FromQuery] int accountId,
        [FromBody] UpdateUserPermissionsRequest request)
    {
        var ok = await _service.UpdatePermissionsAsync(
            id,
            accountId,
            request.Permissions);

        if (!ok)
            return NotFound(ApiResponse<object>.Fail("User not found", 404));

        return Ok(ApiResponse<object>.Ok(null, "Permissions updated"));
    }

    // ----------------------------------------------------
    // PATCH : STATUS
    // ----------------------------------------------------
    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        [FromBody] UpdateUserStatusRequest request)
    {
        var ok = await _service.UpdateStatusAsync(id, request.Status);

        if (!ok)
            return NotFound(ApiResponse<object>.Fail("User not found", 404));

        return Ok(ApiResponse<object>.Ok(null, "Status updated"));
    }

    // ----------------------------------------------------
    // PATCH : 2FA TOGGLE
    // ----------------------------------------------------
    [HttpPatch("{id:guid}/two-factor")]
    public async Task<IActionResult> UpdateTwoFactor(
        Guid id,
        [FromBody] UpdateUserTwoFactorRequest request)
    {
        var ok = await _service.UpdateTwoFactorAsync(id, request.TwoFactorEnabled);

        if (!ok)
            return NotFound(ApiResponse<object>.Fail("User not found", 404));

        return Ok(ApiResponse<object>.Ok(null, "Two factor updated"));
    }

    // ----------------------------------------------------
    // PATCH : PROFILE IMAGE
    // ----------------------------------------------------
    [HttpPatch("{id:guid}/profile-image")]
    public async Task<IActionResult> UpdateProfileImage(
        Guid id,
        IFormFile file)
    {
        if (file == null)
            return BadRequest(ApiResponse<object>.Fail("File is required"));

        var url = await _service.UpdateProfileImageAsync(id, file);

        if (url == null)
            return NotFound(ApiResponse<object>.Fail("User not found", 404));

        return Ok(ApiResponse<object>.Ok(new { profileImageUrl = url }, "Profile image updated"));
    }

    // ----------------------------------------------------
    // PATCH : SEND RESET PASSWORD LINK
    // ----------------------------------------------------
    [HttpPatch("{id:guid}/reset-password")]
    public async Task<IActionResult> SendResetPassword(Guid id)
    {
        var ok = await _service.SendResetPasswordAsync(id);

        if (!ok)
            return NotFound(ApiResponse<object>.Fail("User not found", 404));

        return Ok(ApiResponse<object>.Ok(null, "Password reset link sent"));
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
