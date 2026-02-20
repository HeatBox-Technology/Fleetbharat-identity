using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public class OemManufacturerService : IOemManufacturerService
{
    private readonly IdentityDbContext _db;

    public OemManufacturerService(IdentityDbContext db)
    {
        _db = db;
    }

    public async Task<int> CreateAsync(OemManufacturerDto dto)
    {
        var entity = new OemManufacturer
        {
            Code = dto.Code.Trim(),
            Name = dto.DisplayName.Trim(),
            OfficialWebsite = dto.OfficialWebsite,
            OriginCountry = dto.OriginCountry,
            SupportEmail = dto.SupportEmail,
            SupportHotline = dto.SupportHotline,
            Description = dto.Description,
            IsEnabled = dto.IsEnabled,
            CreatedAt = DateTime.UtcNow
        };

        _db.OemManufacturers.Add(entity);
        await _db.SaveChangesAsync();

        return entity.Id;
    }

    public async Task<OemListUiResponseDto> GetManufacturers(
        int page,
        int pageSize,
        string? search)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 10;

        var query = _db.OemManufacturers
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(x =>
                x.Code.ToLower().Contains(s) ||
                x.Name.ToLower().Contains(s));
        }

        // Summary
        var total = await query.CountAsync();
        var enabled = await query.CountAsync(x => x.IsEnabled);
        var disabled = total - enabled;

        var summary = new OemSummaryDto
        {
            TotalEntities = total,
            Enabled = enabled,
            Disabled = disabled
        };

        var items = await query
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new OemManufacturerDto
            {
                Id = x.Id,
                Code = x.Code,
                DisplayName = x.Name,
                OfficialWebsite = x.OfficialWebsite,
                OriginCountry = x.OriginCountry,
                SupportEmail = x.SupportEmail,
                SupportHotline = x.SupportHotline,
                Description = x.Description,
                IsEnabled = x.IsEnabled,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();

        var totalPages = (int)Math.Ceiling((double)total / pageSize);

        return new OemListUiResponseDto
        {
            Summary = summary,
            Manufacturers = new PagedResultDto<OemManufacturerDto>
            {
                Items = items,
                TotalRecords = total,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages
            }
        };
    }

    public async Task<OemManufacturerDto?> GetByIdAsync(int id)
    {
        return await _db.OemManufacturers
            .Where(x => x.Id == id && !x.IsDeleted)
            .Select(x => new OemManufacturerDto
            {
                Id = x.Id,
                Code = x.Code,
                DisplayName = x.Name,
                OfficialWebsite = x.OfficialWebsite,
                OriginCountry = x.OriginCountry,
                SupportEmail = x.SupportEmail,
                SupportHotline = x.SupportHotline,
                Description = x.Description,
                IsEnabled = x.IsEnabled,
                CreatedAt = x.CreatedAt
            })
            .FirstOrDefaultAsync();
    }

    public async Task<bool> UpdateAsync(int id, OemManufacturerDto dto)
    {
        var entity = await _db.OemManufacturers.FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null) return false;

        entity.Code = dto.Code;
        entity.Name = dto.DisplayName;
        entity.OfficialWebsite = dto.OfficialWebsite;
        entity.OriginCountry = dto.OriginCountry;
        entity.SupportEmail = dto.SupportEmail;
        entity.SupportHotline = dto.SupportHotline;
        entity.Description = dto.Description;
        entity.IsEnabled = dto.IsEnabled;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<bool> UpdateStatusAsync(int id, bool isEnabled)
    {
        var entity = await _db.OemManufacturers.FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null) return false;

        entity.IsEnabled = isEnabled;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var entity = await _db.OemManufacturers.FirstOrDefaultAsync(x => x.Id == id);

        if (entity == null) return false;

        entity.IsDeleted = true;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return true;
    }
}