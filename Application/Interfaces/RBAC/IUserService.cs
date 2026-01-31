using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

public interface IUserService
{
    Task<Guid> CreateAsync(CreateUserRequest req);

    Task<UserListUiResponseDto> GetUsersForUiAsync(
        int page,
        int pageSize,
        int? accountId,
        int? roleId,
        bool? status,
        bool? twoFactorEnabled,
        string? search);

    Task<UserDetailResponseDto?> GetByIdAsync(Guid userId);

    Task<bool> UpdateAsync(Guid userId, UpdateUserRequest req);
    Task<bool> UpdateBasicAsync(Guid userId, UpdateUserBasicRequest req);
    Task<bool> UpdateRoleAsync(Guid userId, UpdateUserRoleRequest req);
    Task<bool> UpdatePermissionsAsync(
        Guid userId,
        int accountId,
        List<UserFormRightDto> permissions);
    Task<bool> UpdateStatusAsync(Guid userId, bool status);
    Task<bool> UpdateTwoFactorAsync(Guid userId, bool enabled);
    Task<bool> SendResetPasswordAsync(Guid userId);
    Task<bool> UpdateProfileImageAsync(Guid userId, IFormFile file);
    Task<bool> SoftDeleteAsync(Guid userId);
}


