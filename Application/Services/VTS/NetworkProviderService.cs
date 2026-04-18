using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public class NetworkProviderService : INetworkProviderService
{
    private readonly IdentityDbContext _db;

    public NetworkProviderService(IdentityDbContext db)
    {
        _db = db;
    }

    public async Task<int> CreateAsync(NetworkProviderDto dto)
    {
        var name = dto.DisplayName.Trim();
        var code = string.IsNullOrWhiteSpace(dto.Code)
            ? name
            : dto.Code.Trim();

        var entity = new NetworkProvider
        {
            Code = code,
            Name = name,
            Description = dto.Description,
            IsEnabled = dto.IsEnabled,
            CreatedAt = DateTime.UtcNow
        };

        _db.NetworkProviders.Add(entity);
        await _db.SaveChangesAsync();

        return entity.Id;
    }

    public async Task<NetworkProviderListUiResponseDto> GetProviders(
        int page,
        int pageSize,
        string? search)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 10;

        var query = _db.NetworkProviders
            .AsNoTracking()
            .Where(x => !x.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(x =>
                (x.Code ?? "").ToLower().Contains(s) ||
                (x.Name ?? "").ToLower().Contains(s) ||
                (x.Description ?? "").ToLower().Contains(s));
        }

        var total = await query.CountAsync();
        var enabled = await query.CountAsync(x => x.IsEnabled);
        var disabled = total - enabled;

        var items = await query
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new NetworkProviderDto
            {
                Id = x.Id,
                Code = string.IsNullOrWhiteSpace(x.Code) ? x.Name : x.Code,
                DisplayName = x.Name,
                Description = x.Description,
                IsEnabled = x.IsEnabled,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync();

        return new NetworkProviderListUiResponseDto
        {
            Summary = new NetworkProviderSummaryDto
            {
                TotalEntities = total,
                Enabled = enabled,
                Disabled = disabled
            },
            Providers = new PagedResultDto<NetworkProviderDto>
            {
                Items = items,
                TotalRecords = total,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)total / pageSize)
            }
        };
    }

    public async Task<NetworkProviderDto?> GetByIdAsync(int id)
    {
        return await _db.NetworkProviders
            .AsNoTracking()
            .Where(x => x.Id == id && !x.IsDeleted)
            .Select(x => new NetworkProviderDto
            {
                Id = x.Id,
                Code = string.IsNullOrWhiteSpace(x.Code) ? x.Name : x.Code,
                DisplayName = x.Name,
                Description = x.Description,
                IsEnabled = x.IsEnabled,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .FirstOrDefaultAsync();
    }

    public async Task<bool> UpdateAsync(int id, NetworkProviderDto dto)
    {
        var entity = await _db.NetworkProviders.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (entity == null)
            return false;

        var name = dto.DisplayName.Trim();
        entity.Code = string.IsNullOrWhiteSpace(dto.Code) ? name : dto.Code.Trim();
        entity.Name = name;
        entity.Description = dto.Description;
        entity.IsEnabled = dto.IsEnabled;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<bool> UpdateStatusAsync(int id, bool isEnabled)
    {
        var entity = await _db.NetworkProviders.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (entity == null)
            return false;

        entity.IsEnabled = isEnabled;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var entity = await _db.NetworkProviders.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        if (entity == null)
            return false;

        entity.IsDeleted = true;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return true;
    }
}
