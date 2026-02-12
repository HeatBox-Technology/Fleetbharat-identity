using Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;


/// <summary>
/// Service for mapping vehicles to devices (historical mapping).
/// Provides CRUD operations for vehicle-device associations.
/// </summary>
public interface IVehicleDeviceMapService
{
    /// <summary>Get all vehicle-device mappings</summary>
    Task<IEnumerable<VehicleDeviceMapDto>> GetAllAsync();
    /// <summary>Get a vehicle-device mapping by ID</summary>
    Task<VehicleDeviceMapDto?> GetByIdAsync(long id);
    /// <summary>Create a new vehicle-device mapping</summary>
    Task<VehicleDeviceMapDto> CreateAsync(VehicleDeviceMapDto dto);
    /// <summary>Update an existing vehicle-device mapping</summary>
    Task<VehicleDeviceMapDto> UpdateAsync(long id, VehicleDeviceMapDto dto);
    /// <summary>Delete a vehicle-device mapping</summary>
    Task<bool> DeleteAsync(long id);
}

