using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs;

public interface IVehicleService
{
    /// <summary>
    /// Create new vehicle
    /// </summary>
    Task<int> CreateAsync(CreateVehicleDto dto);

    /// <summary>
    /// Get vehicles with summary + pagination (UI response)
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
    Task<bool> UpdateAsync(int id, UpdateVehicleDto dto);

    /// <summary>
    /// Update vehicle status
    /// </summary>
    Task<bool> UpdateStatusAsync(int id, string status);

    /// <summary>
    /// Soft delete vehicle
    /// </summary>
    Task<bool> DeleteAsync(int id);

    /// <summary>
    /// Get paged vehicles (without summary)
    /// </summary>
    Task<PagedResultDto<VehicleDto>> GetPagedAsync(
        int page,
        int pageSize,
        string? search = null);

    /// <summary>
    /// Bulk create vehicles
    /// </summary>
    Task<List<VehicleDto>> BulkCreateAsync(List<CreateVehicleDto> vehicles);
}