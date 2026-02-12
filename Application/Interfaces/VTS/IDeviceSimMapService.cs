using Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// Service for mapping devices to SIMs (historical mapping).
/// Provides CRUD operations for device-SIM associations.
/// </summary>
public interface IDeviceSimMapService
{
    /// <summary>Get all device-SIM mappings</summary>
    Task<IEnumerable<DeviceSimMapDto>> GetAllAsync();
    /// <summary>Get a device-SIM mapping by ID</summary>
    Task<DeviceSimMapDto?> GetByIdAsync(long id);
    /// <summary>Create a new device-SIM mapping</summary>
    Task<DeviceSimMapDto> CreateAsync(DeviceSimMapDto dto);
    /// <summary>Update an existing device-SIM mapping</summary>
    Task<DeviceSimMapDto> UpdateAsync(long id, DeviceSimMapDto dto);
    /// <summary>Delete a device-SIM mapping</summary>
    Task<bool> DeleteAsync(long id);
}

