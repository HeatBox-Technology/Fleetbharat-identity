using System;
using System.Threading.Tasks;

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

    Task<bool> SoftDeleteAsync(Guid userId);
}


