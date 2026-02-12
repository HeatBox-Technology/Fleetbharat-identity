using Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;


public interface IDeviceTypeService
{
    Task<IEnumerable<DeviceTypeDto>> GetAllAsync();
    Task<DeviceTypeDto?> GetByIdAsync(int id);
    Task<DeviceTypeDto> CreateAsync(DeviceTypeDto dto);
    Task<DeviceTypeDto> UpdateAsync(int id, DeviceTypeDto dto);
    Task<bool> DeleteAsync(int id);
}

