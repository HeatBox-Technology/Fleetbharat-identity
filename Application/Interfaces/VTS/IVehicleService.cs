using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs;

public interface IVehicleService
{
    /// <summary>
    /// Create new vehicle
    /// </summary>
    Task<int> CreateAsync(VehicleDto dto);

    /// <summary>
    /// Get all vehicles with optional search
    /// </summary>
    Task<VehicleListUiResponseDto> GetVehicles(
     int page,
     int pageSize,
     string? search = null);
    /// <summary>
    /// Get vehicle by id
    /// </summary>
    Task<VehicleDto?> GetByIdAsync(int id);

    /// <summary>
    /// Update vehicle
    /// </summary>
    Task<bool> UpdateAsync(int id, VehicleDto dto);

    /// <summary>
    /// Update vehicle status
    /// </summary>
    Task<bool> UpdateStatusAsync(int id, string status);

    /// <summary>
    /// Delete vehicle
    /// </summary>
    Task<bool> DeleteAsync(int id);

    /// <summary>
    /// Get paged vehicles
    /// </summary>
    Task<PagedResultDto<VehicleDto>> GetPagedAsync(int page, int pageSize);

    /// <summary>
    /// Bulk create vehicles
    /// </summary>
    Task<List<VehicleDto>> BulkCreateAsync(List<VehicleDto> vehicles);
}
