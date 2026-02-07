using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

public class AccountProvisionService : IAccountProvisionService
{

    private readonly IdentityDbContext _db;

    public AccountProvisionService(IdentityDbContext db)
    {
        _db = db;
    }
    public async Task<int> CreateAsync(CreateAccountRequest req)
    {
        using var tx = await _db.Database.BeginTransactionAsync();

        try
        {
            // ✅ validate tax type belongs to country
            var validTax = await _db.TaxTypes.AnyAsync(x =>
                x.TaxTypeId == req.TaxTypeId &&
                x.CountryId == req.CountryId &&
                x.IsActive);

            if (!validTax)
                throw new BadHttpRequestException("Invalid TaxType for selected Country");

            // ✅ account code
            var accountCode = string.IsNullOrWhiteSpace(req.AccountCode)
                ? $"ACC-{DateTime.UtcNow:yyyyMMddHHmmss}"
                : req.AccountCode.Trim();

            var codeExists = await _db.Accounts
                .AnyAsync(x => x.AccountCode == accountCode && !x.IsDeleted);

            if (codeExists)
                throw new InvalidOperationException("AccountCode already exists");

            // -------------------------------------------------
            // ✅ create account
            // -------------------------------------------------

            var account = new mst_account
            {
                AccountCode = accountCode,
                AccountName = req.AccountName.Trim(),
                CategoryId = req.CategoryId,
                PrimaryDomain = req.PrimaryDomain.Trim(),

                CountryId = req.CountryId,
                StateId = req.StateId,
                CityId = req.CityId,
                Zipcode = req.Zipcode,

                ParentAccountId = req.ParentAccountId,
                RefferCode = req.RefferCode,

                TaxTypeId = req.TaxTypeId,

                fullname = req.fullname,
                email = req.email,
                phone = req.phone,
                Position = req.Position,

                address = req.address,

                BusinessPhone = req.BusinessPhone,
                BusinessEmail = req.BusinessEmail,
                BusinessAddress = req.BusinessAddress,
                BusinessHours = req.BusinessHours,
                BusinessTimeZone = req.BusinessTimeZone,

                UserName = req.UserName,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),

                share = req.share,

                Fk_userid = req.userId,
                HierarchyPath = req.HierarchyPath,

                Status = req.Status,

                CreatedOn = DateTime.UtcNow,
                UpdatedOn = DateTime.UtcNow,
                IsDeleted = false,
                CreatedBy = req.userId
            };

            _db.Accounts.Add(account);
            await _db.SaveChangesAsync();


            // -------------------------------------------------
            // ✅ create first user for this account
            // -------------------------------------------------

            var email = req.email?.Trim().ToLower();

            if (!string.IsNullOrWhiteSpace(email))
            {
                var userExists = await _db.Users
                    .AnyAsync(x => x.Email == email && !x.IsDeleted);

                if (userExists)
                    throw new InvalidOperationException("User email already exists");
            }
            int defaultRoleId = 1; // <-- temporary hardcode
            // 👉 get default role for this category (Admin/Owner role)
            // var defaultRoleId = await
            //     (from cr in _db.Categories
            //      join r in _db.Roles on cr.CategoryId equals r.RoleId
            //      where cr.CategoryId == req.CategoryId
            //            && cr.IsActive
            //            && r.   // 👈 recommended column
            //      select r.RoleId)
            //     .FirstOrDefaultAsync();

            if (defaultRoleId == 0)
                throw new InvalidOperationException("No default role configured for this category");

            var user = new User
            {
                UserId = Guid.NewGuid(),
                Email = email ?? "",
                FirstName = req.fullname,
                LastName = "",

                Password_hash = BCrypt.Net.BCrypt.HashPassword(req.Password),

                AccountId = account.AccountId,
                roleId = defaultRoleId,

                MobileNo = req.phone,
                Status = true,
                TwoFactorEnabled = false,

                EmailVerified = false,
                MobileVerified = false,

                CreatedAt = DateTime.UtcNow,
                CreatedBy = req.userId,
                IsDeleted = false
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            await tx.CommitAsync();

            return account.AccountId;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task<AccountListWithCardDto> GetAllAsync(
     int page,
     int pageSize,
     string? search,
     bool? status)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 10;

        // -----------------------------
        // Base query only for cards
        // -----------------------------
        var baseAccountQuery = _db.Accounts
            .Where(x => !x.IsDeleted);

        var cardCounts = await baseAccountQuery
            .GroupBy(x => 1)
            .Select(g => new AccountCardCountDto
            {
                Total = g.Count(),
                Active = g.Count(x => x.Status == true),
                Inactive = g.Count(x => x.Status == false),
                Pending = g.Count(x => x.Status == null)
            })
            .FirstOrDefaultAsync()
            ?? new AccountCardCountDto();

        // -----------------------------
        // Main grid query
        // -----------------------------
        var query =
            from a in _db.Accounts
            join c in _db.Countries on a.CountryId equals c.CountryId

            join st0 in _db.States
                on a.StateId equals st0.StateId.ToString() into stGroup
            from st in stGroup.DefaultIfEmpty()

            join ct0 in _db.Cities
                on a.CityId equals ct0.CityId.ToString() into ctGroup
            from ct in ctGroup.DefaultIfEmpty()

            join cat in _db.Categories on a.CategoryId equals cat.CategoryId

            where !a.IsDeleted
            select new { a, c, cat, st, ct };

        // -----------------------------
        // Search filter
        // -----------------------------
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();

            query = query.Where(x =>
                (x.a.AccountName ?? "").ToLower().Contains(s) ||
                (x.a.AccountCode ?? "").ToLower().Contains(s) ||
                (x.a.PrimaryDomain ?? "").ToLower().Contains(s) ||
                ((x.st != null ? x.st.StateName : "")).ToLower().Contains(s) ||
                ((x.ct != null ? x.ct.CityName : "")).ToLower().Contains(s));
        }

        // -----------------------------
        // Status filter only for grid
        // -----------------------------
        if (status.HasValue)
        {
            query = query.Where(x => x.a.Status == status.Value);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(x => x.a.CreatedOn)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AccountResponseDto
            {
                AccountId = x.a.AccountId,
                AccountCode = x.a.AccountCode,
                AccountName = x.a.AccountName,

                ParentAccountId = x.a.ParentAccountId,
                HierarchyPath = x.a.HierarchyPath,

                Fk_userid = x.a.Fk_userid,

                CategoryId = x.cat.CategoryId,
                CategoryName = x.cat.LabelName,

                PrimaryDomain = x.a.PrimaryDomain,

                CountryId = x.c.CountryId,
                CountryName = x.c.CountryName,

                StateId = x.a.StateId,
                StateName = x.st != null ? x.st.StateName : "",

                CityId = x.a.CityId,
                CityName = x.ct != null ? x.ct.CityName : "",

                fullname = x.a.fullname,
                email = x.a.email,
                phone = x.a.phone,
                address = x.a.address,

                TaxTypeId = x.a.TaxTypeId,
                Status = x.a.Status,

                CreatedOn = x.a.CreatedOn
            })
            .AsNoTracking()
            .ToListAsync();

        return new AccountListWithCardDto
        {
            PageData = new PagedResultDto<AccountResponseDto>
            {
                Page = page,
                PageSize = pageSize,
                TotalRecords = totalCount,
                Items = items
            },
            CardCounts = cardCounts
        };
    }

    public async Task<AccountResponseDto?> GetByIdAsync(int accountId)
    {
        var query =
            from a in _db.Accounts
            join c in _db.Countries on a.CountryId equals c.CountryId

            // ✅ LEFT JOIN mst_state (string -> int match)
            join st in _db.States
                on a.StateId equals st.StateId.ToString() into stj
            from st in stj.DefaultIfEmpty()

                // ✅ LEFT JOIN mst_city (string -> int match)
            join ct in _db.Cities
                on a.CityId equals ct.CityId.ToString() into ctj
            from ct in ctj.DefaultIfEmpty()

            join cat in _db.Categories on a.CategoryId equals cat.CategoryId

            where !a.IsDeleted && a.AccountId == accountId
            select new { a, c, cat, st, ct };

        return await query
            .Select(x => new AccountResponseDto
            {
                AccountId = x.a.AccountId,
                AccountCode = x.a.AccountCode,
                AccountName = x.a.AccountName,

                ParentAccountId = x.a.ParentAccountId,
                HierarchyPath = x.a.HierarchyPath,

                Fk_userid = x.a.Fk_userid,

                CategoryId = x.cat.CategoryId,
                CategoryName = x.cat.LabelName,

                PrimaryDomain = x.a.PrimaryDomain,

                CountryId = x.c.CountryId,
                CountryName = x.c.CountryName,

                // ✅ state
                StateId = x.a.StateId,
                StateName = x.st != null ? x.st.StateName : "",

                // ✅ city
                CityId = x.a.CityId,
                CityName = x.ct != null ? x.ct.CityName : "",

                fullname = x.a.fullname,
                email = x.a.email,
                phone = x.a.phone,
                address = x.a.address,

                TaxTypeId = x.a.TaxTypeId,
                Status = x.a.Status,

                CreatedOn = x.a.CreatedOn
            })
            .AsNoTracking()
            .FirstOrDefaultAsync();
    }

    public async Task<bool> UpdateAsync(int accountId, UpdateAccountRequest req)
    {
        var account = await _db.Accounts
            .FirstOrDefaultAsync(x => x.AccountId == accountId && !x.IsDeleted);

        if (account == null)
            return false;

        // ✅ validate tax type belongs to country
        var validTax = await _db.TaxTypes.AnyAsync(x =>
            x.TaxTypeId == req.TaxTypeId &&
            x.CountryId == req.CountryId &&
            x.IsActive);

        if (!validTax)
            throw new BadHttpRequestException("Invalid TaxType for selected Country");

        // ✅ account code check (only if provided)
        if (!string.IsNullOrWhiteSpace(req.AccountCode))
        {
            var code = req.AccountCode.Trim();

            var codeExists = await _db.Accounts.AnyAsync(x =>
                x.AccountCode == code &&
                x.AccountId != accountId &&
                !x.IsDeleted);

            if (codeExists)
                throw new InvalidOperationException("AccountCode already exists");

            account.AccountCode = code;
        }

        // ------------------------
        // ✅ main fields
        // ------------------------
        account.AccountName = req.AccountName.Trim();
        account.CategoryId = req.CategoryId;
        account.PrimaryDomain = req.PrimaryDomain.Trim();

        account.CountryId = req.CountryId;
        account.StateId = req.StateId;
        account.CityId = req.CityId;
        account.Zipcode = req.Zipcode;

        account.ParentAccountId = req.ParentAccountId;
        account.RefferCode = req.RefferCode;

        account.TaxTypeId = req.TaxTypeId;

        // ------------------------
        // ✅ contact person
        // ------------------------
        account.fullname = req.fullname;
        account.email = req.email;
        account.phone = req.phone;
        account.Position = req.Position;

        account.address = req.address;

        // ------------------------
        // ✅ business profile
        // ------------------------
        account.BusinessPhone = req.BusinessPhone;
        account.BusinessEmail = req.BusinessEmail;
        account.BusinessAddress = req.BusinessAddress;
        account.BusinessHours = req.BusinessHours;
        account.BusinessTimeZone = req.BusinessTimeZone;

        // ------------------------
        // ✅ user access fields
        // ------------------------
        account.UserName = req.UserName;
        account.share = req.share;

        // password only when provided
        if (!string.IsNullOrWhiteSpace(req.Password))
            account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password);

        account.HierarchyPath = req.HierarchyPath;

        account.Status = req.Status;

        account.UpdatedOn = DateTime.UtcNow;
        account.UpdatedBy = req.userId;

        await _db.SaveChangesAsync();
        return true;
    }


    public async Task<bool> UpdateStatusAsync(int accountId, bool status)
    {
        var account = await _db.Accounts.FirstOrDefaultAsync(x => x.AccountId == accountId);
        if (account == null) return false;

        account.Status = status;
        account.UpdatedOn = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int accountId)
    {
        var account = await _db.Accounts.FirstOrDefaultAsync(x => x.AccountId == accountId);
        if (account == null) return false;

        _db.Accounts.Remove(account);
        await _db.SaveChangesAsync();
        return true;
    }
}
