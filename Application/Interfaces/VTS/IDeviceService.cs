using Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IDeviceService
{
    /// <summary>
    /// Gets a paged list of devices.
    /// </summary>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Page size.</param>
    /// <returns>Paged result of devices.</returns>
    Task<PagedResultDto<DeviceDto>> GetPagedAsync(int page, int pageSize);
    Task<IEnumerable<DeviceDto>> GetAllAsync();
    Task<DeviceDto?> GetByIdAsync(int id);
    Task<DeviceDto> CreateAsync(DeviceDto dto);
    Task<DeviceDto> UpdateAsync(int id, DeviceDto dto);
    Task<bool> DeleteAsync(int id);
    /// <summary>
    /// Bulk create devices.
    /// </summary>
    /// <param name="devices">List of devices to create.</param>
    /// <returns>List of created devices.</returns>
    Task<IEnumerable<DeviceDto>> BulkCreateAsync(IEnumerable<DeviceDto> devices);
}

