using Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;


public interface IVehicleService
{
    /// <summary>
    /// Gets a paged list of vehicles.
    /// </summary>
    /// <param name="page">Page number (1-based).</param>
    /// <param name="pageSize">Page size.</param>
    /// <returns>Paged result of vehicles.</returns>
    Task<PagedResultDto<VehicleDto>> GetPagedAsync(int page, int pageSize);
    Task<IEnumerable<VehicleDto>> GetAllAsync();
    Task<VehicleDto?> GetByIdAsync(int id);
    Task<VehicleDto> CreateAsync(VehicleDto dto);
    Task<VehicleDto> UpdateAsync(int id, VehicleDto dto);
    Task<bool> DeleteAsync(int id);
    /// <summary>
    /// Bulk create vehicles.
    /// </summary>
    /// <param name="vehicles">List of vehicles to create.</param>
    /// <returns>List of created vehicles.</returns>
    Task<IEnumerable<VehicleDto>> BulkCreateAsync(IEnumerable<VehicleDto> vehicles);
}

