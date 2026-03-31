using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Npgsql;
public class UserService : IUserService
{
    private readonly IdentityDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileStorageService _fileStorage;
    private readonly IConfiguration _config;

    public UserService(
        IdentityDbContext db,
        ICurrentUserService currentUser,
        IFileStorageService fileStorage,
        IConfiguration config)
    {
        _db = db;
        _currentUser = currentUser;
        _fileStorage = fileStorage;
        _config = config;
    }
    public async Task<Guid> CreateAsync(CreateUserRequest req)
    {
        using var tx = await _db.Database.BeginTransactionAsync();

        try
        {
            var utcNow = DateTime.UtcNow;
            var actorAccountId = _currentUser.AccountId > 0 ? _currentUser.AccountId : (int?)null;
            var email = req.Email.Trim().ToLowerInvariant();
            var password = req.Password?.Trim() ?? string.Empty;

            ValidatePasswordOrThrow(password);

            // Scope email uniqueness to the target account.
            if (await _db.Users.AnyAsync(x =>
                x.AccountId == req.AccountId &&
                x.Email == email &&
                !x.IsDeleted))
                throw new InvalidOperationException("Email already exists");

            var account = await _db.Accounts
                .ApplyAccountHierarchyFilter(_currentUser)
                .FirstOrDefaultAsync(x => x.AccountId == req.AccountId && !x.IsDeleted);

            if (account == null)
                throw new KeyNotFoundException("Account not found");

            var categoryName = await _db.Categories
                .Where(x => x.CategoryId == account.CategoryId && !x.IsDeleted)
                .Select(x => x.LabelName)
                .FirstOrDefaultAsync();

            var targetRoleName = ResolveDefaultRoleByCategory(categoryName);
            if (string.IsNullOrWhiteSpace(targetRoleName))
                throw new BadHttpRequestException("No default role mapping found for account category");

            // Role creation is the only path that may require an earlier save because RoleId is DB-generated.
            var role = await GetOrCreateRoleAsync(req.AccountId, targetRoleName, utcNow, actorAccountId);

            var userId = Guid.NewGuid();

            string? imagePath = null;
            if (req.ProfileImage != null && req.ProfileImage.Length > 0)
                imagePath = await _fileStorage.SaveProfileImageAsync(userId, req.ProfileImage);

            var user = new User
            {
                UserId = userId,
                Email = email,
                User_name = string.IsNullOrWhiteSpace(req.UserName) ? email : req.UserName.Trim(),
                FirstName = req.FirstName.Trim(),
                LastName = req.LastName.Trim(),
                Password_hash = BCrypt.Net.BCrypt.HashPassword(password),

                AccountId = req.AccountId,
                roleId = role.RoleId,
                MobileNo = req.MobileNo?.Trim() ?? string.Empty,
                Status = req.Status,
                TwoFactorEnabled = req.TwoFactorEnabled,
                ProfileImagePath = imagePath,
                EmailVerified = false,
                MobileVerified = false,

                CreatedAt = utcNow,
                UpdatedAt = utcNow,
                CreatedBy = actorAccountId,
                UpdatedBy = actorAccountId,
                IsDeleted = false
            };

            _db.Users.Add(user);

            if (req.AdditionalPermissions?.Any() == true)
            {
                foreach (var p in req.AdditionalPermissions)
                {
                    _db.UserFormRights.Add(new map_user_form_right
                    {
                        UserId = userId,
                        AccountId = req.AccountId,
                        FormId = p.FormId,
                        CanRead = p.CanRead,
                        CanWrite = p.CanWrite,
                        CanUpdate = p.CanUpdate,
                        CanDelete = p.CanDelete,
                        CanExport = p.CanExport,
                        CanAll = p.CanAll
                    });
                }
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return userId;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    private async Task<mst_role> GetOrCreateRoleAsync(
        int accountId,
        string roleName,
        DateTime utcNow,
        int? actorAccountId)
    {
        var normalizedRoleName = roleName.Trim();

        var role = await _db.Roles
            .ApplyAccountHierarchyFilter(_currentUser)
            .FirstOrDefaultAsync(x =>
                x.AccountId == accountId &&
                x.RoleName == normalizedRoleName &&
                !x.IsDeleted);

        if (role != null)
            return role;

        role = new mst_role
        {
            RoleName = normalizedRoleName,
            RoleCode = BuildRoleCode(normalizedRoleName),
            AccountId = accountId,
            IsSystemRole = false,
            IsActive = true,
            CreatedOn = utcNow,
            UpdatedOn = utcNow,
            CreatedBy = actorAccountId,
            UpdatedBy = actorAccountId
        };

        _db.Roles.Add(role);

        try
        {
            await _db.SaveChangesAsync();
            return role;
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            _db.Entry(role).State = EntityState.Detached;

            var existingRole = await _db.Roles
                .ApplyAccountHierarchyFilter(_currentUser)
                .FirstOrDefaultAsync(x =>
                    x.AccountId == accountId &&
                    x.RoleName == normalizedRoleName &&
                    !x.IsDeleted);

            if (existingRole != null)
                return existingRole;

            throw new InvalidOperationException("Role creation failed due to a concurrent update.", ex);
        }
    }

    private string ResolveDefaultRoleByCategory(string? categoryName)
    {
        if (string.IsNullOrWhiteSpace(categoryName))
            return string.Empty;

        var normalized = categoryName.Trim().ToLowerInvariant();

        foreach (var mapping in GetRoleMappings())
        {
            if (normalized.Contains(mapping.Key))
                return mapping.Value;
        }

        return string.Empty;
    }

    private IEnumerable<KeyValuePair<string, string>> GetRoleMappings()
    {
        var configuredMappings = _config
            .GetSection("RoleMappings:AccountCategories")
            .GetChildren()
            .Select(x => new KeyValuePair<string, string>(
                x.Key.Trim().ToLowerInvariant(),
                (x.Value ?? string.Empty).Trim()))
            .Where(x => !string.IsNullOrWhiteSpace(x.Key) && !string.IsNullOrWhiteSpace(x.Value))
            .ToList();

        if (configuredMappings.Count > 0)
            return configuredMappings;

        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["distributor"] = "DistributorAdmin",
            ["reseller"] = "ResellerAdmin",
            ["dealer"] = "DealerAdmin"
        };
    }

    private static string BuildRoleCode(string roleName)
    {
        return new string(roleName
            .Trim()
            .ToUpperInvariant()
            .Where(char.IsLetterOrDigit)
            .ToArray());
    }

    private static void ValidatePasswordOrThrow(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new InvalidOperationException("Password is required");

        if (password.Length < 8)
            throw new InvalidOperationException("Password must be at least 8 characters long");

        if (!password.Any(char.IsUpper))
            throw new InvalidOperationException("Password must contain at least one uppercase letter");

        if (!password.Any(char.IsLower))
            throw new InvalidOperationException("Password must contain at least one lowercase letter");

        if (!password.Any(char.IsDigit))
            throw new InvalidOperationException("Password must contain at least one number");

        if (!password.Any(ch => !char.IsLetterOrDigit(ch)))
            throw new InvalidOperationException("Password must contain at least one special character");
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        return ex.InnerException is PostgresException postgresException &&
               postgresException.SqlState == PostgresErrorCodes.UniqueViolation;
    }


    // ✅ 1) GET ALL USERS (UI LIST + SUMMARY)
    // public async Task<UserListUiResponseDto> GetUsersForUiAsync(
    //     int page,
    //     int pageSize,
    //     int? accountId,
    //     int? roleId,
    //     bool? status,
    //     bool? twoFactorEnabled,
    //     string? search)
    // {
    //     if (page <= 0) page = 1;
    //     if (pageSize <= 0) pageSize = 10;

    //     var baseQuery = _db.Users.AsNoTracking()
    //         .ApplyAccountHierarchyFilter(_currentUser)
    //         .Where(x => !x.IsDeleted)
    //         .AsQueryable();

    //     if (accountId.HasValue)
    //         baseQuery = baseQuery.Where(x => x.AccountId == accountId.Value);

    //     if (roleId.HasValue)
    //         baseQuery = baseQuery.Where(x => x.roleId == roleId.Value);

    //     if (status.HasValue)
    //         baseQuery = baseQuery.Where(x => x.Status == status.Value);

    //     if (twoFactorEnabled.HasValue)
    //         baseQuery = baseQuery.Where(x => x.TwoFactorEnabled == twoFactorEnabled.Value);

    //     if (!string.IsNullOrWhiteSpace(search))
    //     {
    //         var s = search.Trim().ToLower();
    //         baseQuery = baseQuery.Where(x =>
    //             x.FirstName.ToLower().Contains(s) ||
    //             x.LastName.ToLower().Contains(s) ||
    //             x.Email.ToLower().Contains(s) ||
    //             x.MobileNo.ToLower().Contains(s));
    //     }

    //     // ✅ SUMMARY CARDS
    //     var totalUsers = await baseQuery.CountAsync();
    //     var active = await baseQuery.CountAsync(x => x.Status == true);
    //     var suspended = await baseQuery.CountAsync(x => x.Status == false);
    //     var twoFa = await baseQuery.CountAsync(x => x.TwoFactorEnabled == true);

    //     var summary = new UserCardSummaryDto
    //     {
    //         TotalUsers = totalUsers,
    //         Active = active,
    //         SuspendedOrLocked = suspended,
    //         TwoFactorEnabled = twoFa
    //     };

    //     // ✅ TABLE DATA (Join Role + Account to show names)
    //     var tableQuery =
    //         from u in baseQuery
    //         join r in _db.Roles.AsNoTracking().ApplyAccountHierarchyFilter(_currentUser) on u.roleId equals r.RoleId
    //         join a in _db.Accounts.AsNoTracking().ApplyAccountHierarchyFilter(_currentUser) on u.AccountId equals a.AccountId
    //         select new UserListItemDto
    //         {
    //             UserId = u.UserId,
    //             FullName = (u.FirstName + " " + u.LastName).Trim(),
    //             Email = u.Email,

    //             RoleId = r.RoleId,
    //             RoleName = r.RoleName,

    //             AccountId = a.AccountId,
    //             AccountName = a.AccountName,
    //             profileImagePath = u.ProfileImagePath,


    //             Status = u.Status,
    //             TwoFactorEnabled = u.TwoFactorEnabled,
    //             LastLoginAt = u.LastLoginAt
    //         };

    //     var totalRecords = await tableQuery.CountAsync();

    //     var items = await tableQuery
    //         .OrderByDescending(x => x.LastLoginAt ?? DateTime.MinValue)
    //         .Skip((page - 1) * pageSize)
    //         .Take(pageSize)
    //         .ToListAsync();

    //     return new UserListUiResponseDto
    //     {
    //         Summary = summary,
    //         Users = new PagedResultDto<UserListItemDto>
    //         {
    //             Page = page,
    //             PageSize = pageSize,
    //             TotalRecords = totalRecords,
    //             Items = items
    //         }
    //     };
    // }

    public async Task<UserListUiResponseDto> GetUsersForUiAsync(
        int page,
        int pageSize,
        int? accountId,
        int? roleId,
        bool? status,
        bool? twoFactorEnabled,
        string? search)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 10;

        var baseQuery = _db.Users.AsNoTracking().AsQueryable();

        // Only apply hierarchy filter for non-system account
        if (_currentUser.AccountId != 1)
        {
            baseQuery = baseQuery.ApplyAccountHierarchyFilter(_currentUser);
        }

        baseQuery = baseQuery.Where(x => !x.IsDeleted);

        if (accountId.HasValue)
            baseQuery = baseQuery.Where(x => x.AccountId == accountId.Value);

        if (roleId.HasValue)
            baseQuery = baseQuery.Where(x => x.roleId == roleId.Value);

        if (status.HasValue)
            baseQuery = baseQuery.Where(x => x.Status == status.Value);

        if (twoFactorEnabled.HasValue)
            baseQuery = baseQuery.Where(x => x.TwoFactorEnabled == twoFactorEnabled.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();

            baseQuery = baseQuery.Where(x =>
                (x.FirstName ?? "").ToLower().Contains(s) ||
                (x.LastName ?? "").ToLower().Contains(s) ||
                (x.Email ?? "").ToLower().Contains(s) ||
                (x.MobileNo ?? "").ToLower().Contains(s));
        }

        var totalUsers = await baseQuery.CountAsync();
        var active = await baseQuery.CountAsync(x => x.Status == true);
        var suspended = await baseQuery.CountAsync(x => x.Status == false);
        var twoFa = await baseQuery.CountAsync(x => x.TwoFactorEnabled == true);

        var summary = new UserCardSummaryDto
        {
            TotalUsers = totalUsers,
            Active = active,
            SuspendedOrLocked = suspended,
            TwoFactorEnabled = twoFa
        };

        var roleQuery = _db.Roles.AsNoTracking().AsQueryable();
        var accountQuery = _db.Accounts.AsNoTracking().AsQueryable();

        if (_currentUser.AccountId != 1)
        {
            roleQuery = roleQuery.ApplyAccountHierarchyFilter(_currentUser);
            accountQuery = accountQuery.ApplyAccountHierarchyFilter(_currentUser);
        }

        var tableQuery =
            from u in baseQuery

            join r0 in roleQuery
                on u.roleId equals r0.RoleId into roleGroup
            from r in roleGroup.DefaultIfEmpty()

            join a0 in accountQuery
                on u.AccountId equals a0.AccountId into accountGroup
            from a in accountGroup.DefaultIfEmpty()

            select new UserListItemDto
            {
                UserId = u.UserId,
                FullName = ((u.FirstName ?? "") + " " + (u.LastName ?? "")).Trim(),
                Email = u.Email,

                RoleId = r != null ? r.RoleId : 0,
                RoleName = r != null ? r.RoleName : "",

                AccountId = a != null ? a.AccountId : 0,
                AccountName = a != null ? a.AccountName : "",

                profileImagePath = u.ProfileImagePath,

                Status = u.Status,
                TwoFactorEnabled = u.TwoFactorEnabled,
                LastLoginAt = u.LastLoginAt
            };

        var totalRecords = await tableQuery.CountAsync();

        var items = await tableQuery
            .OrderByDescending(x => x.LastLoginAt ?? DateTime.MinValue)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new UserListUiResponseDto
        {
            Summary = summary,
            Users = new PagedResultDto<UserListItemDto>
            {
                Page = page,
                PageSize = pageSize,
                TotalRecords = totalRecords,
                Items = items
            }
        };
    }



    // ✅ 2) GET USER BY ID
    public async Task<UserDetailResponseDto?> GetByIdAsync(Guid userId)
    {
        var userQuery = _db.Users.AsNoTracking().AsQueryable();

        if (_currentUser.AccountId != 1)
        {
            userQuery = userQuery.ApplyAccountHierarchyFilter(_currentUser);
        }

        var u = await userQuery
            .FirstOrDefaultAsync(x => x.UserId == userId && !x.IsDeleted);

        if (u == null)
            return null;
        var permissionQuery = _db.UserFormRights.AsNoTracking().AsQueryable();

        if (_currentUser.AccountId != 1)
        {
            permissionQuery = permissionQuery.ApplyAccountHierarchyFilter(_currentUser);
        }

        var permissions = await permissionQuery
            .Where(x => x.UserId == userId && x.AccountId == u.AccountId)
            .Select(x => new UserFormRightDto
            {
                FormId = x.FormId,
                CanRead = x.CanRead,
                CanWrite = x.CanWrite,
                CanUpdate = x.CanUpdate,
                CanDelete = x.CanDelete,
                CanExport = x.CanExport,
                CanAll = x.CanAll
            })
            .ToListAsync();

        return new UserDetailResponseDto
        {
            UserId = u.UserId,
            FirstName = u.FirstName,
            LastName = u.LastName,
            UserName = u.User_name ?? "",
            Email = u.Email,
            MobileNo = u.MobileNo,
            CountryCode = u.CountryCode,
            AccountId = u.AccountId,
            RoleId = u.roleId,
            Status = u.Status,
            TwoFactorEnabled = u.TwoFactorEnabled,
            profileImagePath = u.ProfileImagePath,
            CreatedAt = u.CreatedAt,
            UpdatedAt = u.UpdatedAt,

            // ✅ added
            AdditionalPermissions = permissions
        };
    }


    // ✅ 3) UPDATE USER
    public async Task<bool> UpdateAsync(Guid userId, UpdateUserRequest req)
    {
        using var tx = await _db.Database.BeginTransactionAsync();

        try
        {
            var user = await _db.Users
                .ApplyAccountHierarchyFilter(_currentUser)
                .FirstOrDefaultAsync(x => x.UserId == userId && !x.IsDeleted);

            if (user == null)
                return false;

            // ✅ validate account exists
            if (!await _db.Accounts.AnyAsync(x => x.AccountId == req.AccountId && !x.IsDeleted))
                throw new KeyNotFoundException("Account not found");

            // ✅ validate role belongs to account
            if (!await _db.Roles.AnyAsync(x =>
                x.RoleId == req.RoleId && x.AccountId == req.AccountId))
                throw new InvalidOperationException("Role is not valid for this account");


            // -------------------------
            // ✅ profile image handling
            // -------------------------
            if (req.ProfileImage != null && req.ProfileImage.Length > 0)
            {
                user.ProfileImagePath = await _fileStorage.SaveProfileImageAsync(user.UserId, req.ProfileImage);
            }


            // -------------------------
            // ✅ user basic fields
            // -------------------------
            user.FirstName = req.FirstName.Trim();
            user.LastName = req.LastName.Trim();
            user.MobileNo = req.MobileNo.Trim();
            user.CountryCode = req.CountryCode.Trim();

            user.AccountId = req.AccountId;
            user.roleId = req.RoleId;
            user.User_name = req.UserName.Trim();

            user.Status = req.Status;
            user.TwoFactorEnabled = req.TwoFactorEnabled;

            // Update password only when request contains a non-empty Password field.
            var passwordProp = req.GetType().GetProperty("Password");
            if (passwordProp?.PropertyType == typeof(string))
            {
                var newPassword = passwordProp.GetValue(req) as string;
                if (!string.IsNullOrEmpty(newPassword))
                {
                    user.Password_hash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                }
            }

            user.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();


            // -------------------------
            // ✅ update additional permissions
            // -------------------------
            if (req.AdditionalPermissions != null)
            {
                // remove old permissions
                var existing = await _db.UserFormRights
                    .ApplyAccountHierarchyFilter(_currentUser)
                    .Where(x => x.UserId == userId && x.AccountId == req.AccountId)
                    .ToListAsync();

                if (existing.Any())
                    _db.UserFormRights.RemoveRange(existing);

                // add new permissions
                if (req.AdditionalPermissions.Any())
                {
                    foreach (var p in req.AdditionalPermissions)
                    {
                        _db.UserFormRights.Add(new map_user_form_right
                        {
                            UserId = userId,
                            AccountId = req.AccountId,
                            FormId = p.FormId,
                            CanRead = p.CanRead,
                            CanWrite = p.CanWrite,
                            CanUpdate = p.CanUpdate,
                            CanDelete = p.CanDelete,
                            CanExport = p.CanExport,
                            CanAll = p.CanAll
                        });
                    }

                    await _db.SaveChangesAsync();
                }
            }

            await tx.CommitAsync();
            return true;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }


    // 4) split updates into small PATCH APIs as needed
    public async Task<bool> UpdateBasicAsync(Guid userId, UpdateUserBasicRequest req)
    {
        var user = await _db.Users
            .ApplyAccountHierarchyFilter(_currentUser)
            .FirstOrDefaultAsync(x => x.UserId == userId && !x.IsDeleted);

        if (user == null) return false;

        user.FirstName = req.FirstName.Trim();
        user.LastName = req.LastName.Trim();
        user.MobileNo = req.MobileNo.Trim();
        user.CountryCode = req.CountryCode.Trim();
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }
    public async Task<bool> UpdateRoleAsync(Guid userId, UpdateUserRoleRequest req)
    {
        var user = await _db.Users
            .ApplyAccountHierarchyFilter(_currentUser)
            .FirstOrDefaultAsync(x => x.UserId == userId && !x.IsDeleted);

        if (user == null) return false;

        var roleExists = await _db.Roles
            .ApplyAccountHierarchyFilter(_currentUser)
            .AnyAsync(x => x.RoleId == req.RoleId && x.AccountId == req.AccountId);

        if (!roleExists)
            throw new InvalidOperationException("Role not valid for this account");

        user.AccountId = req.AccountId;
        user.roleId = req.RoleId;
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }
    public async Task<bool> UpdatePermissionsAsync(Guid userId, int accountId,
        List<UserFormRightDto> permissions)
    {
        using var tx = await _db.Database.BeginTransactionAsync();

        var user = await _db.Users
            .ApplyAccountHierarchyFilter(_currentUser)
            .FirstOrDefaultAsync(x => x.UserId == userId && !x.IsDeleted);

        if (user == null) return false;

        var existing = await _db.UserFormRights
            .ApplyAccountHierarchyFilter(_currentUser)
            .Where(x => x.UserId == userId)
            .ToListAsync();

        _db.UserFormRights.RemoveRange(existing);

        foreach (var p in permissions)
        {
            _db.UserFormRights.Add(new map_user_form_right
            {
                UserId = userId,
                AccountId = accountId,
                FormId = p.FormId,
                CanRead = p.CanRead,
                CanWrite = p.CanWrite,
                CanUpdate = p.CanUpdate,
                CanDelete = p.CanDelete,
                CanExport = p.CanExport,
                CanAll = p.CanAll
            });
        }

        await _db.SaveChangesAsync();
        await tx.CommitAsync();
        return true;
    }
    public async Task<bool> UpdateStatusAsync(Guid userId, bool status)
    {
        var user = await _db.Users
            .ApplyAccountHierarchyFilter(_currentUser)
            .FirstOrDefaultAsync(x => x.UserId == userId && !x.IsDeleted);

        if (user == null) return false;

        user.Status = status;
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }
    public async Task<bool> UpdateTwoFactorAsync(Guid userId, bool enabled)
    {
        var user = await _db.Users
            .ApplyAccountHierarchyFilter(_currentUser)
            .FirstOrDefaultAsync(x => x.UserId == userId && !x.IsDeleted);

        if (user == null) return false;

        user.TwoFactorEnabled = enabled;

        if (!enabled)
        {
            user.TwoFactorCodeHash = null;
            user.TwoFactorExpiry = null;
        }

        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }
    public async Task<string?> UpdateProfileImageAsync(Guid userId, IFormFile file)
    {
        var user = await _db.Users
            .ApplyAccountHierarchyFilter(_currentUser)
            .FirstOrDefaultAsync(x => x.UserId == userId && !x.IsDeleted);

        if (user == null) return null;

        user.ProfileImagePath = await _fileStorage.SaveProfileImageAsync(userId, file);
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return user.ProfileImagePath;
    }
    public async Task<bool> SendResetPasswordAsync(Guid userId)
    {
        var user = await _db.Users
            .ApplyAccountHierarchyFilter(_currentUser)
            .FirstOrDefaultAsync(x => x.UserId == userId && !x.IsDeleted);

        if (user == null) return false;

        // Generate reset token
        var token = Guid.NewGuid().ToString();
        user.PasswordResetTokenHash = BCrypt.Net.BCrypt.HashPassword(token);
        user.PasswordResetExpiry = DateTime.UtcNow.AddHours(1);
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    // 5) SOFT DELETE
    public async Task<bool> SoftDeleteAsync(Guid userId)
    {
        var user = await _db.Users
            .ApplyAccountHierarchyFilter(_currentUser)
            .FirstOrDefaultAsync(x => x.UserId == userId && !x.IsDeleted);
        if (user == null) return false;

        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }
}

