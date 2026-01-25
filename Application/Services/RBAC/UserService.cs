using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Linq;
public class UserService : IUserService
{
    private readonly IdentityDbContext _db;

    public UserService(IdentityDbContext db)
    {
        _db = db;
    }

    public async Task<Guid> CreateAsync(CreateUserRequest req)
    {
        var email = req.Email.Trim().ToLower();

        var exists = await _db.Users.AnyAsync(x => x.Email == email);
        if (exists)
            throw new InvalidOperationException("Email already exists");

        // ✅ validate account
        var accExists = await _db.Accounts.AnyAsync(x => x.AccountId == req.AccountId);
        if (!accExists)
            throw new KeyNotFoundException("Account not found");

        // ✅ validate role must belong to same account
        var roleExists = await _db.Roles.AnyAsync(x => x.RoleId == req.RoleId && x.AccountId == req.AccountId);
        if (!roleExists)
            throw new BadHttpRequestException("Role not valid for this account");

        var user = new User
        {
            UserId = Guid.NewGuid(), // ✅ important

            Email = email,
            FirstName = req.FirstName.Trim(),
            LastName = req.LastName.Trim(),

            Password_hash = BCrypt.Net.BCrypt.HashPassword(req.Password),

            AccountId = req.AccountId,
            roleId = req.RoleId,

            CreatedAt = DateTime.UtcNow,
            Status = true
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return user.UserId;
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
        var u = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId && !x.IsDeleted);

        if (u == null) return null;

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
            CreatedAt = u.CreatedAt,
            UpdatedAt = u.UpdatedAt
        };
    }

    // ✅ 3) UPDATE USER
    public async Task<bool> UpdateAsync(Guid userId, UpdateUserRequest req)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(x => x.UserId == userId && !x.IsDeleted);

        if (user == null) return false;

        // ✅ validate account exists
        var accountExists = await _db.Accounts.AnyAsync(x => x.AccountId == req.AccountId);
        if (!accountExists) throw new KeyNotFoundException("Account not found");

        // ✅ validate role belongs to account
        var roleExists = await _db.Roles.AnyAsync(x =>
            x.RoleId == req.RoleId && x.AccountId == req.AccountId);

        if (!roleExists) throw new InvalidOperationException("Role is not valid for this account");

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
        return true;
    }

    // ✅ 4) SOFT DELETE
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

