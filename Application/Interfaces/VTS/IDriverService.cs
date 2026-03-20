using Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IDriverService
{
    /// <summary>
    /// Get drivers with summary + pagination
    /// </summary>
    Task<DriverListUiResponseDto> GetDrivers(
        int page,
        int pageSize,
        int? accountId = null,
        string? search = null);

    /// <summary>
    /// Get paged drivers only
    /// </summary>
    Task<PagedResultDto<DriverDto>> GetPagedAsync(
        int page,
        int pageSize,
        int? accountId = null,
        string? search = null);

    /// <summary>
    /// Get all drivers
    /// </summary>
    Task<IEnumerable<DriverDto>> GetAllAsync(int page = 1, int pageSize = 10, string? search = null);

    /// <summary>
    /// Get driver by id
    /// </summary>
    Task<DriverDto?> GetByIdAsync(int driverId);

    /// <summary>
    /// Create driver
    /// </summary>
    Task<int> CreateAsync(CreateDriverDto dto);

    /// <summary>
    /// Update driver
    /// </summary>
    Task<bool> UpdateAsync(int driverId, UpdateDriverDto dto);

    /// <summary>
    /// Update driver status
    /// </summary>
    Task<bool> UpdateStatusAsync(int driverId, bool isActive);

    /// <summary>
    /// Soft delete driver
    /// </summary>
    Task<bool> DeleteAsync(int driverId);

    /// <summary>
    /// Bulk create drivers
    /// </summary>
    Task<List<DriverDto>> BulkCreateAsync(List<CreateDriverDto> drivers);
    Task<byte[]> ExportDriversCsvAsync(int? accountId, string? search);
}
