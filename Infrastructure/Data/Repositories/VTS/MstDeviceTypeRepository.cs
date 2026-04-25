using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public class MstDeviceTypeRepository : IMstDeviceTypeRepository
{
    private readonly IdentityDbContext _db;

    public MstDeviceTypeRepository(IdentityDbContext db)
    {
        _db = db;
    }

    public Task AddAsync(mst_device_type entity)
    {
        return _db.DeviceTypes.AddAsync(entity).AsTask();
    }

    public Task<bool> CodeExistsAsync(string code, int? excludeId = null)
    {
        var query = _db.DeviceTypes
            .AsNoTracking()
            .Where(x => x.Code == code && !x.IsDeleted);

        if (excludeId.HasValue)
            query = query.Where(x => x.Id != excludeId.Value);

        return query.AnyAsync();
    }

    public Task<List<mst_device_type>> GetAllActiveAsync(string? search = null)
    {
        var query = _db.DeviceTypes
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(x =>
                x.Code.ToLower().Contains(term) ||
                x.Name.ToLower().Contains(term) ||
                (x.Description ?? string.Empty).ToLower().Contains(term));
        }

        return query
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .ToListAsync();
    }

    public Task<mst_device_type?> GetActiveByIdAsync(int id)
    {
        return _db.DeviceTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted && x.IsActive);
    }

    public Task<mst_device_type?> GetByIdForWriteAsync(int id)
    {
        return _db.DeviceTypes
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
    }

    public Task<bool> OemManufacturerExistsAsync(int oemManufacturerId)
    {
        return _db.OemManufacturers
            .AsNoTracking()
            .AnyAsync(x => x.Id == oemManufacturerId && !x.IsDeleted && x.IsEnabled);
    }

    public Task<int> SaveChangesAsync()
    {
        return _db.SaveChangesAsync();
    }
}
