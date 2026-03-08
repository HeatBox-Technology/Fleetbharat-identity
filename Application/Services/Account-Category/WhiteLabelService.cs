using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

public class WhiteLabelService : IWhiteLabelService
{
    private readonly IdentityDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileStorageService _fileStorage;

    public WhiteLabelService(IdentityDbContext db, ICurrentUserService currentUser, IFileStorageService fileStorage)
    {
        _db = db;
        _currentUser = currentUser;
        _fileStorage = fileStorage;
    }

    public async Task<int> CreateAsync(CreateWhiteLabelRequest req)
    {
        var account = await _db.Accounts
            .ApplyAccountHierarchyFilter(_currentUser)
            .FirstOrDefaultAsync(x => x.AccountId == req.AccountId);
        if (account == null)
            throw new KeyNotFoundException("Account not found");

        var exists = await _db.WhiteLabels
            .ApplyAccountHierarchyFilter(_currentUser)
            .AnyAsync(x => x.AccountId == req.AccountId && !x.IsDeleted);
        if (exists)
            throw new InvalidOperationException("WhiteLabel already provisioned for this account");

        // ✅ Basic FQDN clean
        var fqdn = req.CustomEntryFqdn.Trim().ToLower();
        fqdn = fqdn.Replace("https://", "").Replace("http://", "");

        var entity = new mst_white_label
        {
            AccountId = req.AccountId,
            CustomEntryFqdn = fqdn,
            BrandName = req.BrandName,
            LogoName = req.LogoName,
            LogoPath = req.LogoPath,
            LogoUrl = req.LogoUrl,
            PrimaryColorHex = string.IsNullOrWhiteSpace(req.PrimaryColorHex) ? "#4F46E5" : req.PrimaryColorHex,
            SecondaryColorHex = string.IsNullOrWhiteSpace(req.PrimaryColorHex) ? "#bbbace" : req.SecondaryColorHex,
            IsActive = req.IsActive,
            CreatedOn = DateTime.UtcNow
        };

        _db.WhiteLabels.Add(entity);
        await _db.SaveChangesAsync();

        return entity.WhiteLabelId;
    }

    public async Task<bool> UpdateAsync(int id, UpdateWhiteLabelRequest req)
    {
        var entity = await _db.WhiteLabels
            .ApplyAccountHierarchyFilter(_currentUser)
            .FirstOrDefaultAsync(x => x.WhiteLabelId == id && !x.IsDeleted);
        if (entity == null) return false;

        var fqdn = req.CustomEntryFqdn.Trim().ToLower();
        fqdn = fqdn.Replace("https://", "").Replace("http://", "");

        entity.CustomEntryFqdn = fqdn;
        entity.BrandName = req.BrandName;
        entity.LogoUrl = req.LogoUrl;
        entity.LogoName = req.LogoName;
        entity.LogoPath = req.LogoPath;
        entity.PrimaryColorHex = string.IsNullOrWhiteSpace(req.PrimaryColorHex) ? entity.PrimaryColorHex : req.PrimaryColorHex;
        entity.SecondaryColorHex = string.IsNullOrWhiteSpace(req.SecondaryColorHex) ? entity.SecondaryColorHex : req.SecondaryColorHex;
        entity.IsActive = req.IsActive;
        entity.UpdatedOn = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var entity = await _db.WhiteLabels
            .ApplyAccountHierarchyFilter(_currentUser)
            .FirstOrDefaultAsync(x => x.WhiteLabelId == id);
        if (entity == null) return false;

        entity.IsDeleted = true;
        entity.UpdatedOn = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<WhiteLabelResponseDto?> GetByIdAsync(int id)
    {
        return await (
            from wl in _db.WhiteLabels.AsNoTracking().ApplyAccountHierarchyFilter(_currentUser)
            join acc in _db.Accounts.AsNoTracking().ApplyAccountHierarchyFilter(_currentUser) on wl.AccountId equals acc.AccountId
            where wl.WhiteLabelId == id && !wl.IsDeleted
            select new WhiteLabelResponseDto
            {
                WhiteLabelId = wl.WhiteLabelId,
                AccountId = wl.AccountId,
                AccountName = acc.AccountName,

                CustomEntryFqdn = wl.CustomEntryFqdn,
                BrandName = wl.BrandName,
                LogoUrl = wl.LogoUrl,
                LogoName = wl.LogoName,
                LogoPath = wl.LogoPath,
                PrimaryColorHex = wl.PrimaryColorHex,
                SecondaryColorHex = wl.SecondaryColorHex,

                IsActive = wl.IsActive,
                CreatedOn = wl.CreatedOn,
                UpdatedOn = wl.UpdatedOn
            }
        ).FirstOrDefaultAsync();
    }

    public async Task<WhiteLabelResponseDto?> GetByAccountIdAsync(int accountId)
    {
        return await (
            from wl in _db.WhiteLabels.AsNoTracking().ApplyAccountHierarchyFilter(_currentUser)
            join acc in _db.Accounts.AsNoTracking().ApplyAccountHierarchyFilter(_currentUser) on wl.AccountId equals acc.AccountId
            where wl.AccountId == accountId && !wl.IsDeleted
            select new WhiteLabelResponseDto
            {
                WhiteLabelId = wl.WhiteLabelId,
                AccountId = wl.AccountId,
                AccountName = acc.AccountName,

                CustomEntryFqdn = wl.CustomEntryFqdn,
                BrandName = wl.BrandName,
                LogoUrl = wl.LogoUrl,
                LogoName = wl.LogoName,
                LogoPath = wl.LogoPath,
                PrimaryColorHex = wl.PrimaryColorHex,
                SecondaryColorHex = wl.SecondaryColorHex,

                IsActive = wl.IsActive,
                CreatedOn = wl.CreatedOn,
                UpdatedOn = wl.UpdatedOn
            }
        ).FirstOrDefaultAsync();
    }

    public async Task<PagedResultDto<WhiteLabelResponseDto>> GetAllAsync(
        int page,
        int pageSize,
        string? search,
        bool? isActive)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 10;

        var query =
            from wl in _db.WhiteLabels.AsNoTracking().ApplyAccountHierarchyFilter(_currentUser)
            join acc in _db.Accounts.AsNoTracking().ApplyAccountHierarchyFilter(_currentUser) on wl.AccountId equals acc.AccountId
            where !wl.IsDeleted
            select new { wl, acc };

        if (isActive.HasValue)
            query = query.Where(x => x.wl.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(x =>
                x.acc.AccountName.ToLower().Contains(s) ||
                x.wl.CustomEntryFqdn.ToLower().Contains(s));
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(x => x.wl.UpdatedOn ?? x.wl.CreatedOn)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new WhiteLabelResponseDto
            {
                WhiteLabelId = x.wl.WhiteLabelId,
                AccountId = x.wl.AccountId,
                AccountName = x.acc.AccountName,
                CustomEntryFqdn = x.wl.CustomEntryFqdn,
                BrandName = x.wl.BrandName,
                LogoUrl = x.wl.LogoUrl,
                LogoName = x.wl.LogoName,
                LogoPath = x.wl.LogoPath,
                PrimaryColorHex = x.wl.PrimaryColorHex,
                SecondaryColorHex = x.wl.SecondaryColorHex,
                IsActive = x.wl.IsActive,
                CreatedOn = x.wl.CreatedOn,
                UpdatedOn = x.wl.UpdatedOn
            })
            .ToListAsync();

        return new PagedResultDto<WhiteLabelResponseDto>
        {
            Page = page,
            PageSize = pageSize,
            TotalRecords = total,
            Items = items
        };
    }

    public async Task<WhiteLabelLogoUploadResponseDto> UploadLogoAsync(int accountId, IFormFile file)
    {
        var account = await _db.Accounts
            .ApplyAccountHierarchyFilter(_currentUser)
            .FirstOrDefaultAsync(x => x.AccountId == accountId && !x.IsDeleted);

        if (account == null)
            throw new KeyNotFoundException("Account not found");

        var whiteLabel = await _db.WhiteLabels
            .ApplyAccountHierarchyFilter(_currentUser)
            .FirstOrDefaultAsync(x => x.AccountId == accountId && !x.IsDeleted);

        if (whiteLabel == null)
        {
            whiteLabel = new mst_white_label
            {
                AccountId = accountId,
                CustomEntryFqdn = account.PrimaryDomain,
                BrandName = account.AccountName,
                IsActive = true,
                CreatedOn = DateTime.UtcNow
            };

            _db.WhiteLabels.Add(whiteLabel);
            await _db.SaveChangesAsync();
        }

        var logoPath = await _fileStorage.SaveWhiteLabelLogoAsync(accountId, file);

        whiteLabel.BrandName ??= account.AccountName;
        whiteLabel.LogoPath = logoPath;
        whiteLabel.LogoName = $"{accountId}.png";
        whiteLabel.LogoUrl = logoPath;
        whiteLabel.UpdatedOn = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return new WhiteLabelLogoUploadResponseDto
        {
            WhiteLabelId = whiteLabel.WhiteLabelId,
            AccountId = accountId,
            BrandName = whiteLabel.BrandName,
            LogoName = whiteLabel.LogoName,
            LogoPath = whiteLabel.LogoPath,
            FileUrl = whiteLabel.LogoPath
        };
    }
}
