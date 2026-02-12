using Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;


/// <summary>
/// Service for mapping vehicles to sensors (mount points).
/// </summary>
public interface IVehicleSensorMapService
{
    Task<IEnumerable<VehicleSensorMapDto>> GetAllAsync();
    Task<VehicleSensorMapDto?> GetByIdAsync(long id);
    Task<VehicleSensorMapDto> CreateAsync(VehicleSensorMapDto dto);
    Task<VehicleSensorMapDto> UpdateAsync(long id, VehicleSensorMapDto dto);
    Task<bool> DeleteAsync(long id);
}

