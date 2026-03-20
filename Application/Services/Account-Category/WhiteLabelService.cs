using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

public class WhiteLabelService : IWhiteLabelService
{
    private const long MaxLogoFileSizeBytes = 2 * 1024 * 1024; // 2 MB
    private static readonly HashSet<string> AllowedLogoContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png",
        "image/jpg",
        "image/jpeg"
    };

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
                PrimaryLogoPath = wl.PrimaryLogoPath,
                PrimaryLogoUrl = wl.PrimaryLogoUrl,
                AppLogoPath = wl.AppLogoPath,
                AppLogoUrl = wl.AppLogoUrl,
                MobileLogoPath = wl.MobileLogoPath,
                MobileLogoUrl = wl.MobileLogoUrl,
                FaviconPath = wl.FaviconPath,
                FaviconUrl = wl.FaviconUrl,
                LogoDarkPath = wl.LogoDarkPath,
                LogoDarkUrl = wl.LogoDarkUrl,
                LogoLightPath = wl.LogoLightPath,
                LogoLightUrl = wl.LogoLightUrl,
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
                PrimaryLogoPath = wl.PrimaryLogoPath,
                PrimaryLogoUrl = wl.PrimaryLogoUrl,
                AppLogoPath = wl.AppLogoPath,
                AppLogoUrl = wl.AppLogoUrl,
                MobileLogoPath = wl.MobileLogoPath,
                MobileLogoUrl = wl.MobileLogoUrl,
                FaviconPath = wl.FaviconPath,
                FaviconUrl = wl.FaviconUrl,
                LogoDarkPath = wl.LogoDarkPath,
                LogoDarkUrl = wl.LogoDarkUrl,
                LogoLightPath = wl.LogoLightPath,
                LogoLightUrl = wl.LogoLightUrl,
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
                PrimaryLogoPath = x.wl.PrimaryLogoPath,
                PrimaryLogoUrl = x.wl.PrimaryLogoUrl,
                AppLogoPath = x.wl.AppLogoPath,
                AppLogoUrl = x.wl.AppLogoUrl,
                MobileLogoPath = x.wl.MobileLogoPath,
                MobileLogoUrl = x.wl.MobileLogoUrl,
                FaviconPath = x.wl.FaviconPath,
                FaviconUrl = x.wl.FaviconUrl,
                LogoDarkPath = x.wl.LogoDarkPath,
                LogoDarkUrl = x.wl.LogoDarkUrl,
                LogoLightPath = x.wl.LogoLightPath,
                LogoLightUrl = x.wl.LogoLightUrl,
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
        return await UploadLogosAsync(new WhiteLabelLogoUploadRequest
        {
            AccountId = accountId,
            PrimaryLogo = file
        });
    }

    public async Task<WhiteLabelLogoUploadResponseDto> UploadLogosAsync(WhiteLabelLogoUploadRequest req)
    {
        if (!HasAnyLogo(req))
            throw new InvalidOperationException("At least one logo file is required.");

        var account = await _db.Accounts
            .ApplyAccountHierarchyFilter(_currentUser)
            .FirstOrDefaultAsync(x => x.AccountId == req.AccountId && !x.IsDeleted);

        if (account == null)
            throw new KeyNotFoundException("Account not found");

        var whiteLabel = await _db.WhiteLabels
            .ApplyAccountHierarchyFilter(_currentUser)
            .FirstOrDefaultAsync(x => x.AccountId == req.AccountId && !x.IsDeleted);

        if (whiteLabel == null)
        {
            whiteLabel = new mst_white_label
            {
                AccountId = req.AccountId,
                CustomEntryFqdn = account.PrimaryDomain,
                BrandName = account.AccountName,
                IsActive = true,
                CreatedOn = DateTime.UtcNow
            };

            _db.WhiteLabels.Add(whiteLabel);
            await _db.SaveChangesAsync();
        }

        if (req.PrimaryLogo != null && req.PrimaryLogo.Length > 0)
        {
            ValidateLogoFile(req.PrimaryLogo, "PrimaryLogo");
            var path = await _fileStorage.SavePrimaryLogoAsync(req.AccountId, req.PrimaryLogo);
            whiteLabel.PrimaryLogoPath = path;
            whiteLabel.PrimaryLogoUrl = path;

            // Backward compatibility with legacy single-logo fields.
            whiteLabel.LogoPath = path;
            whiteLabel.LogoUrl = path;
            whiteLabel.LogoName = Path.GetFileName(path);
        }

        if (req.AppLogo != null && req.AppLogo.Length > 0)
        {
            ValidateLogoFile(req.AppLogo, "AppLogo");
            var path = await _fileStorage.SaveAppLogoAsync(req.AccountId, req.AppLogo);
            whiteLabel.AppLogoPath = path;
            whiteLabel.AppLogoUrl = path;
        }

        if (req.MobileLogo != null && req.MobileLogo.Length > 0)
        {
            ValidateLogoFile(req.MobileLogo, "MobileLogo");
            var path = await _fileStorage.SaveMobileLogoAsync(req.AccountId, req.MobileLogo);
            whiteLabel.MobileLogoPath = path;
            whiteLabel.MobileLogoUrl = path;
        }

        if (req.Favicon != null && req.Favicon.Length > 0)
        {
            ValidateLogoFile(req.Favicon, "Favicon");
            var path = await _fileStorage.SaveFaviconAsync(req.AccountId, req.Favicon);
            whiteLabel.FaviconPath = path;
            whiteLabel.FaviconUrl = path;
        }

        if (req.LogoDark != null && req.LogoDark.Length > 0)
        {
            ValidateLogoFile(req.LogoDark, "LogoDark");
            var path = await _fileStorage.SaveDarkLogoAsync(req.AccountId, req.LogoDark);
            whiteLabel.LogoDarkPath = path;
            whiteLabel.LogoDarkUrl = path;
        }

        if (req.LogoLight != null && req.LogoLight.Length > 0)
        {
            ValidateLogoFile(req.LogoLight, "LogoLight");
            var path = await _fileStorage.SaveLightLogoAsync(req.AccountId, req.LogoLight);
            whiteLabel.LogoLightPath = path;
            whiteLabel.LogoLightUrl = path;
        }

        whiteLabel.BrandName ??= account.AccountName;
        whiteLabel.UpdatedOn = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return new WhiteLabelLogoUploadResponseDto
        {
            WhiteLabelId = whiteLabel.WhiteLabelId,
            AccountId = req.AccountId,
            PrimaryLogoUrl = whiteLabel.PrimaryLogoUrl,
            AppLogoUrl = whiteLabel.AppLogoUrl,
            MobileLogoUrl = whiteLabel.MobileLogoUrl,
            FaviconUrl = whiteLabel.FaviconUrl,
            LogoDarkUrl = whiteLabel.LogoDarkUrl,
            LogoLightUrl = whiteLabel.LogoLightUrl
        };
    }

    private static void ValidateLogoFile(IFormFile file, string logoType)
    {
        if (file == null || file.Length == 0)
            throw new InvalidOperationException($"{logoType} file is required.");

        if (!AllowedLogoContentTypes.Contains(file.ContentType))
            throw new InvalidOperationException($"{logoType} format is invalid. Allowed formats: PNG, JPG, JPEG.");

        if (file.Length > MaxLogoFileSizeBytes)
            throw new InvalidOperationException($"{logoType} file size must be 2 MB or less.");
    }

    private static bool HasAnyLogo(WhiteLabelLogoUploadRequest req)
    {
        return (req.PrimaryLogo?.Length ?? 0) > 0 ||
               (req.AppLogo?.Length ?? 0) > 0 ||
               (req.MobileLogo?.Length ?? 0) > 0 ||
               (req.Favicon?.Length ?? 0) > 0 ||
               (req.LogoDark?.Length ?? 0) > 0 ||
               (req.LogoLight?.Length ?? 0) > 0;
    }

}
