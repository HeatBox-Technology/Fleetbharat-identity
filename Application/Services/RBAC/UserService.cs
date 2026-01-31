using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.IO;
public class UserService : IUserService
{
    private readonly IdentityDbContext _db;

    public UserService(IdentityDbContext db)
    {
        _db = db;
    }
    public async Task<Guid> CreateAsync(CreateUserRequest req)
    {
        using var tx = await _db.Database.BeginTransactionAsync();

        try
        {
            var email = req.Email.Trim().ToLower();

            // 1️⃣ Email uniqueness
            if (await _db.Users.AnyAsync(x => x.Email == email && !x.IsDeleted))
                throw new InvalidOperationException("Email already exists");

            // 2️⃣ Account validation
            if (!await _db.Accounts.AnyAsync(x => x.AccountId == req.AccountId && !x.IsDeleted))
                throw new KeyNotFoundException("Account not found");

            // 3️⃣ Role validation
            if (!await _db.Roles.AnyAsync(x => x.RoleId == req.RoleId && x.AccountId == req.AccountId))
                throw new BadHttpRequestException("Role not valid for this account");

            var userId = Guid.NewGuid();

            // 4️⃣ Handle profile image (if provided)
            string? imagePath = null;

            if (req.ProfileImage != null && req.ProfileImage.Length > 0)
            {
                var allowedTypes = new[] { "image/jpeg", "image/png" };
                if (!allowedTypes.Contains(req.ProfileImage.ContentType))
                    throw new InvalidOperationException("Only JPG and PNG images are allowed");

                if (req.ProfileImage.Length > 2 * 1024 * 1024)
                    throw new InvalidOperationException("Image size must be less than 2MB");

                var uploadsRoot = Path.Combine("uploads", "users", userId.ToString());
                Directory.CreateDirectory(uploadsRoot);

                var filePath = Path.Combine(uploadsRoot, "profile.jpg");

                using var stream = new FileStream(filePath, FileMode.Create);
                await req.ProfileImage.CopyToAsync(stream);

                imagePath = "/" + filePath.Replace("\\", "/"); // for URL use
            }

            // 5️⃣ Create user
            var user = new User
            {
                UserId = userId,
                Email = email,
                FirstName = req.FirstName.Trim(),
                LastName = req.LastName.Trim(),
                Password_hash = BCrypt.Net.BCrypt.HashPassword(req.Password),

                AccountId = req.AccountId,
                roleId = req.RoleId,
                MobileNo = req.MobileNo,
                Status = req.Status,
                TwoFactorEnabled = req.TwoFactorEnabled,
                ProfileImagePath = imagePath,
                EmailVerified = false,
                MobileVerified = false,

                CreatedAt = DateTime.UtcNow,
                CreatedBy = null,
                IsDeleted = false
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            // 6️⃣ Additional permissions
            if (req.AdditionalPermissions?.Any() == true)
            {
                foreach (var p in req.AdditionalPermissions)
                {
                    _db.UserFormRights.Add(new map_UserFormRight
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

            await tx.CommitAsync();
            return userId;
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            throw;
        }
    }


    // ✅ 1) GET ALL USERS (UI LIST + SUMMARY)
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

        var baseQuery = _db.Users.AsNoTracking()
            .Where(x => !x.IsDeleted)
            .AsQueryable();

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
                x.FirstName.ToLower().Contains(s) ||
                x.LastName.ToLower().Contains(s) ||
                x.Email.ToLower().Contains(s) ||
                x.MobileNo.ToLower().Contains(s));
        }

        // ✅ SUMMARY CARDS
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

        // ✅ TABLE DATA (Join Role + Account to show names)
        var tableQuery =
            from u in baseQuery
            join r in _db.Roles.AsNoTracking() on u.roleId equals r.RoleId
            join a in _db.Accounts.AsNoTracking() on u.AccountId equals a.AccountId
            select new UserListItemDto
            {
                UserId = u.UserId,
                FullName = (u.FirstName + " " + u.LastName).Trim(),
                Email = u.Email,

                RoleId = r.RoleId,
                RoleName = r.RoleName,

                AccountId = a.AccountId,
                AccountName = a.AccountName,
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
        var u = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId && !x.IsDeleted);

        if (u == null)
            return null;

        var permissions = await _db.UserFormRights
            .AsNoTracking()
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
                var allowedTypes = new[] { "image/jpeg", "image/png" };

                if (!allowedTypes.Contains(req.ProfileImage.ContentType))
                    throw new InvalidOperationException("Only JPG and PNG images are allowed");

                if (req.ProfileImage.Length > 2 * 1024 * 1024)
                    throw new InvalidOperationException("Image size must be less than 2MB");

                var uploadsRoot = Path.Combine("uploads", "users", user.UserId.ToString());
                Directory.CreateDirectory(uploadsRoot);

                var filePath = Path.Combine(uploadsRoot, "profile.jpg");

                using var stream = new FileStream(filePath, FileMode.Create);
                await req.ProfileImage.CopyToAsync(stream);

                user.ProfileImagePath = "/" + filePath.Replace("\\", "/");
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

            user.Status = req.Status;
            user.TwoFactorEnabled = req.TwoFactorEnabled;

            user.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();


            // -------------------------
            // ✅ update additional permissions
            // -------------------------
            if (req.AdditionalPermissions != null)
            {
                // remove old permissions
                var existing = await _db.UserFormRights
                    .Where(x => x.UserId == userId && x.AccountId == req.AccountId)
                    .ToListAsync();

                if (existing.Any())
                    _db.UserFormRights.RemoveRange(existing);

                // add new permissions
                if (req.AdditionalPermissions.Any())
                {
                    foreach (var p in req.AdditionalPermissions)
                    {
                        _db.UserFormRights.Add(new map_UserFormRight
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
            .FirstOrDefaultAsync(x => x.UserId == userId && !x.IsDeleted);

        if (user == null) return false;

        var roleExists = await _db.Roles
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
            .FirstOrDefaultAsync(x => x.UserId == userId && !x.IsDeleted);

        if (user == null) return false;

        var existing = await _db.UserFormRights
            .Where(x => x.UserId == userId)
            .ToListAsync();

        _db.UserFormRights.RemoveRange(existing);

        foreach (var p in permissions)
        {
            _db.UserFormRights.Add(new map_UserFormRight
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
    public async Task<bool> UpdateProfileImageAsync(Guid userId, IFormFile file)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(x => x.UserId == userId && !x.IsDeleted);

        if (user == null) return false;

        var allowed = new[] { "image/jpeg", "image/png" };

        if (!allowed.Contains(file.ContentType))
            throw new InvalidOperationException("Invalid image type");

        if (file.Length > 2 * 1024 * 1024)
            throw new InvalidOperationException("Image too large");

        var root = Path.Combine("uploads", "users", userId.ToString());
        Directory.CreateDirectory(root);

        var filePath = Path.Combine(root, "profile.jpg");

        using var fs = new FileStream(filePath, FileMode.Create);
        await file.CopyToAsync(fs);

        user.ProfileImagePath = "/" + filePath.Replace("\\", "/");
        user.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }
    public async Task<bool> SendResetPasswordAsync(Guid userId)
    {
        var user = await _db.Users
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
        var user = await _db.Users.FirstOrDefaultAsync(x => x.UserId == userId && !x.IsDeleted);
        if (user == null) return false;

        user.IsDeleted = true;
        user.DeletedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }
}

