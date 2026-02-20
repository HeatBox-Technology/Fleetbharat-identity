using System.Collections.Generic;
using System.Threading.Tasks;
using Application.DTOs;

/// <summary>
/// Service contract for device management.
/// </summary>
public interface IDeviceService
{
    /// <summary>
    /// Create a new device.
    /// </summary>
    Task<int> CreateAsync(DeviceDto dto);

    /// <summary>
    /// Get devices with summary cards and pagination.
    /// </summary>
    Task<DeviceListUiResponseDto> GetDevices(
        int page,
        int pageSize,
        int? accountId,
        string? search);

    /// <summary>
    /// Get device by Id.
    /// </summary>
    Task<DeviceDto?> GetByIdAsync(int id);

    /// <summary>
    /// Update device.
    /// </summary>
    Task<bool> UpdateAsync(int id, DeviceDto dto);

    /// <summary>
    /// Update device status only.
    /// </summary>
    Task<bool> UpdateStatusAsync(int id, string status);

    /// <summary>
    /// Soft delete device.
    /// </summary>
    Task<bool> DeleteAsync(int id);

    /// <summary>
    /// Bulk create devices.
    /// </summary>
    Task<List<DeviceDto>> BulkCreateAsync(List<DeviceDto> devices);
}