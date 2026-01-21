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
        // ✅ validate tax type belongs to country
        var validTax = await _db.TaxTypes.AnyAsync(x =>
            x.TaxTypeId == req.TaxTypeId &&
            x.CountryId == req.CountryId &&
            x.IsActive);

        if (!validTax)
            throw new BadHttpRequestException("Invalid TaxType for selected Country");

        var accountCode = string.IsNullOrWhiteSpace(req.AccountCode)
            ? $"ACC-{DateTime.UtcNow:yyyyMMddHHmmss}"
            : req.AccountCode.Trim();

        var codeExists = await _db.Accounts.AnyAsync(x => x.AccountCode == accountCode);
        if (codeExists)
            throw new InvalidOperationException("AccountCode already exists");

        var account = new mst_account
        {
            AccountCode = accountCode,
            AccountName = req.AccountName.Trim(),
            CategoryId = req.CategoryId,
            PrimaryDomain = req.PrimaryDomain.Trim(),
            CountryId = req.CountryId,
            TaxTypeId = req.TaxTypeId,
            ParentAccountId = req.ParentAccountId,
            HierarchyPath = req.HierarchyPath,
            Fk_userid = req.userId,
            Status = req.Status,
            CreatedOn = DateTime.UtcNow,
            UpdatedOn = DateTime.UtcNow
        };

        _db.Accounts.Add(account);
        await _db.SaveChangesAsync();

        return account.AccountId;
    }

    public async Task<PagedResultDto<AccountResponseDto>> GetAllAsync(
        int page,
        int pageSize,
        string? search,
        bool? status)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 10;

        var query = _db.Accounts.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(x =>
                x.AccountName.ToLower().Contains(s) ||
                x.AccountCode.ToLower().Contains(s) ||
                x.PrimaryDomain.ToLower().Contains(s));
        }

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(x => x.CreatedOn)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AccountResponseDto
            {
                AccountId = x.AccountId,
                AccountCode = x.AccountCode,
                AccountName = x.AccountName,
                ParentAccountId = x.ParentAccountId,
                HierarchyPath = x.HierarchyPath,
                Fk_userid = x.Fk_userid,
                CategoryId = x.CategoryId,
                PrimaryDomain = x.PrimaryDomain,
                CountryId = x.CountryId,
                TaxTypeId = x.TaxTypeId,
                Status = x.Status,
                CreatedOn = x.CreatedOn
            })
            .ToListAsync();

        return new PagedResultDto<AccountResponseDto>
        {
            Page = page,
            PageSize = pageSize,
            TotalRecords = totalCount,
            Items = items
        };
    }

    public async Task<AccountResponseDto?> GetByIdAsync(int accountId)
    {
        return await _db.Accounts
            .Where(x => x.AccountId == accountId)
            .Select(x => new AccountResponseDto
            {
                AccountId = x.AccountId,
                AccountCode = x.AccountCode,
                AccountName = x.AccountName,
                ParentAccountId = x.ParentAccountId,
                HierarchyPath = x.HierarchyPath,
                Fk_userid = x.Fk_userid,
                CategoryId = x.CategoryId,
                PrimaryDomain = x.PrimaryDomain,
                CountryId = x.CountryId,
                TaxTypeId = x.TaxTypeId,
                Status = x.Status,
                CreatedOn = x.CreatedOn
            })
            .FirstOrDefaultAsync();
    }

    public async Task<bool> UpdateAsync(int accountId, UpdateAccountRequest req)
    {
        var account = await _db.Accounts.FirstOrDefaultAsync(x => x.AccountId == accountId);
        if (account == null) return false;

        // ✅ validate tax type belongs to country
        var validTax = await _db.TaxTypes.AnyAsync(x =>
            x.TaxTypeId == req.TaxTypeId &&
            x.CountryId == req.CountryId &&
            x.IsActive);

        if (!validTax)
            throw new BadHttpRequestException("Invalid TaxType for selected Country");

        // ✅ check code duplicate (excluding same account)
        var codeExists = await _db.Accounts.AnyAsync(x =>
            x.AccountCode == req.AccountCode.Trim() && x.AccountId != accountId);

        if (codeExists)
            throw new InvalidOperationException("AccountCode already exists");

        account.AccountName = req.AccountName.Trim();
        account.AccountCode = req.AccountCode.Trim();
        account.CategoryId = req.CategoryId;
        account.PrimaryDomain = req.PrimaryDomain.Trim();
        account.CountryId = req.CountryId;
        account.TaxTypeId = req.TaxTypeId;
        account.ParentAccountId = req.ParentAccountId;
        account.HierarchyPath = req.HierarchyPath;
        account.Fk_userid = req.userId;
        account.Status = req.Status;
        account.UpdatedOn = DateTime.UtcNow;

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
