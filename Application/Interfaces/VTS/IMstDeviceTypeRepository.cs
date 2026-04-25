using System.Collections.Generic;
using System.Threading.Tasks;

public interface IMstDeviceTypeRepository
{
    Task AddAsync(mst_device_type entity);
    Task<bool> CodeExistsAsync(string code, int? excludeId = null);
    Task<List<mst_device_type>> GetAllActiveAsync(string? search = null);
    Task<mst_device_type?> GetActiveByIdAsync(int id);
    Task<mst_device_type?> GetByIdForWriteAsync(int id);
    Task<bool> OemManufacturerExistsAsync(int oemManufacturerId);
    Task<int> SaveChangesAsync();
}
