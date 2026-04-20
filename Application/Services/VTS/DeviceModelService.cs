using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public class DeviceModelService : IDeviceModelService
{
    private readonly IdentityDbContext _db;

    public DeviceModelService(IdentityDbContext db)
    {
        _db = db;
    }

    public async Task<int> CreateAsync(CreateDeviceModelDto dto)
    {
        var code = dto.Code.Trim().ToUpperInvariant();
        var name = dto.DisplayName.Trim();
        var protocolType = dto.ProtocolType.Trim().ToUpperInvariant();

        var exists = await _db.DeviceModels
            .AnyAsync(x => x.Code == code && !x.IsDeleted);

        if (exists)
            throw new InvalidOperationException("Device model already exists.");

        await ValidateReferences(dto.ManufacturerId, dto.DeviceCategoryId);

        var entity = new DeviceModel
        {
            Code = code,
            Name = name,
            Description = dto.Description,
            ManufacturerId = dto.ManufacturerId,
            DeviceCategoryId = dto.DeviceCategoryId,
            ProtocolType = protocolType,
            IsEnabled = dto.IsEnabled,
            CreatedAt = DateTime.UtcNow
        };

        _db.DeviceModels.Add(entity);
        await _db.SaveChangesAsync();

        return entity.Id;
    }

    public async Task<DeviceModelListUiResponseDto> GetModels(
        int page,
        int pageSize,
        string? search)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 10;

        var query =
            from model in _db.DeviceModels.AsNoTracking()
            join manufacturer in _db.OemManufacturers.AsNoTracking()
                on model.ManufacturerId equals manufacturer.Id into manufacturers
            from manufacturer in manufacturers.DefaultIfEmpty()
            join category in _db.DeviceTypes.AsNoTracking()
                on model.DeviceCategoryId equals category.Id into categories
            from category in categories.DefaultIfEmpty()
            where !model.IsDeleted
            select new DeviceModelProjection
            {
                Id = model.Id,
                Code = model.Code,
                Name = model.Name,
                Description = model.Description,
                ManufacturerId = model.ManufacturerId,
                ManufacturerName = manufacturer != null ? manufacturer.Name : string.Empty,
                DeviceCategoryId = model.DeviceCategoryId,
                DeviceCategoryName = category != null ? category.Name : string.Empty,
                ProtocolType = model.ProtocolType,
                IsEnabled = model.IsEnabled,
                CreatedAt = model.CreatedAt,
                UpdatedAt = model.UpdatedAt
            };

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(x =>
                x.Code.ToLower().Contains(s) ||
                x.Name.ToLower().Contains(s) ||
                (x.Description ?? string.Empty).ToLower().Contains(s) ||
                x.ManufacturerName.ToLower().Contains(s) ||
                x.DeviceCategoryName.ToLower().Contains(s) ||
                x.ProtocolType.ToLower().Contains(s));
        }

        var total = await query.CountAsync();
        var enabled = await query.CountAsync(x => x.IsEnabled);
        var disabled = total - enabled;

        var items = await query
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new DeviceModelDto
            {
                Id = x.Id,
                Code = x.Code,
                DisplayName = x.Name,
                Description = x.Description,
                ManufacturerId = x.ManufacturerId,
                ManufacturerName = x.ManufacturerName,
                DeviceCategoryId = x.DeviceCategoryId,
                DeviceCategoryName = x.DeviceCategoryName,
                ProtocolType = x.ProtocolType,
                IsEnabled = x.IsEnabled,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt
            })
            .ToListAsync();

        return new DeviceModelListUiResponseDto
        {
            Summary = new DeviceModelSummaryDto
            {
                TotalEntities = total,
                Enabled = enabled,
                Disabled = disabled
            },
            Models = new PagedResultDto<DeviceModelDto>
            {
                Items = items,
                TotalRecords = total,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling((double)total / pageSize)
            }
        };
    }

    public async Task<DeviceModelDto?> GetByIdAsync(int id)
    {
        return await (
            from model in _db.DeviceModels.AsNoTracking()
            join manufacturer in _db.OemManufacturers.AsNoTracking()
                on model.ManufacturerId equals manufacturer.Id into manufacturers
            from manufacturer in manufacturers.DefaultIfEmpty()
            join category in _db.DeviceTypes.AsNoTracking()
                on model.DeviceCategoryId equals category.Id into categories
            from category in categories.DefaultIfEmpty()
            where model.Id == id && !model.IsDeleted
            select new DeviceModelDto
            {
                Id = model.Id,
                Code = model.Code,
                DisplayName = model.Name,
                Description = model.Description,
                ManufacturerId = model.ManufacturerId,
                ManufacturerName = manufacturer != null ? manufacturer.Name : string.Empty,
                DeviceCategoryId = model.DeviceCategoryId,
                DeviceCategoryName = category != null ? category.Name : string.Empty,
                ProtocolType = model.ProtocolType,
                IsEnabled = model.IsEnabled,
                CreatedAt = model.CreatedAt,
                UpdatedAt = model.UpdatedAt
            })
            .FirstOrDefaultAsync();
    }

    public async Task<bool> UpdateAsync(int id, UpdateDeviceModelDto dto)
    {
        var entity = await _db.DeviceModels
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (entity == null)
            return false;

        var code = dto.Code.Trim().ToUpperInvariant();
        var exists = await _db.DeviceModels
            .AnyAsync(x => x.Code == code && x.Id != id && !x.IsDeleted);

        if (exists)
            throw new InvalidOperationException("Device model already exists.");

        await ValidateReferences(dto.ManufacturerId, dto.DeviceCategoryId);

        entity.Code = code;
        entity.Name = dto.DisplayName.Trim();
        entity.Description = dto.Description;
        entity.ManufacturerId = dto.ManufacturerId;
        entity.DeviceCategoryId = dto.DeviceCategoryId;
        entity.ProtocolType = dto.ProtocolType.Trim().ToUpperInvariant();
        entity.IsEnabled = dto.IsEnabled;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateStatusAsync(int id, bool isEnabled)
    {
        var entity = await _db.DeviceModels
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (entity == null)
            return false;

        entity.IsEnabled = isEnabled;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var entity = await _db.DeviceModels
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (entity == null)
            return false;

        entity.IsDeleted = true;
        entity.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    private async Task ValidateReferences(int manufacturerId, int deviceCategoryId)
    {
        var manufacturerExists = await _db.OemManufacturers
            .AnyAsync(x => x.Id == manufacturerId && !x.IsDeleted && x.IsEnabled);

        if (!manufacturerExists)
            throw new InvalidOperationException("Manufacturer not found or disabled.");

        var categoryExists = await _db.DeviceTypes
            .AnyAsync(x => x.Id == deviceCategoryId && !x.IsDeleted && x.IsEnabled);

        if (!categoryExists)
            throw new InvalidOperationException("Device category not found or disabled.");
    }

    private class DeviceModelProjection
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int ManufacturerId { get; set; }
        public string ManufacturerName { get; set; } = string.Empty;
        public int DeviceCategoryId { get; set; }
        public string DeviceCategoryName { get; set; } = string.Empty;
        public string ProtocolType { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
