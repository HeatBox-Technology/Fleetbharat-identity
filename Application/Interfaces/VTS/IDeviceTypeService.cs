using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs;

/// <summary>
/// Service contract for device type management.
/// </summary>
public interface IDeviceTypeService
{
    /// <summary>
    /// Create new device type
    /// </summary>
    Task<int> CreateAsync(CreateDeviceTypeDto dto);

    /// <summary>
    /// Get device types with summary + pagination (UI screen)
    /// </summary>
    Task<DeviceTypeListUiResponseDto> GetDeviceTypes(
        int page,
        int pageSize,
        string? search = null);

    /// <summary>
    /// Get paged device types only
    /// </summary>
    Task<PagedResultDto<DeviceTypeDto>> GetPagedAsync(
        int page,
        int pageSize,
        string? search = null);


    /// <summary>
    /// Get device type by Id
    /// </summary>
    Task<DeviceTypeDto?> GetByIdAsync(int id);

    /// <summary>
    /// Update device type
    /// </summary>
    Task<bool> UpdateAsync(int id, UpdateDeviceTypeDto dto);

    /// <summary>
    /// Update active status
    /// </summary>
    Task<bool> UpdateStatusAsync(int id, bool isActive);

    /// <summary>
    /// Soft delete device type
    /// </summary>
    Task<bool> DeleteAsync(int id);

    /// <summary>
    /// Bulk create device types
    /// </summary>
    Task<List<DeviceTypeDto>> BulkCreateAsync(List<CreateDeviceTypeDto> items);


}