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
    Task<int> CreateAsync(CreateDeviceDto dto);

    /// <summary>
    /// Get devices with summary cards and pagination.
    /// </summary>
    Task<DeviceListUiResponseDto> GetDevices(
        int page,
        int pageSize,
        int? accountId = null,
        string? search = null);

    /// <summary>
    /// Get device by Id.
    /// </summary>
    Task<DeviceDto?> GetByIdAsync(int id);

    /// <summary>
    /// Update device.
    /// </summary>
    Task<bool> UpdateAsync(int id, UpdateDeviceDto dto);

    /// <summary>
    /// Update device status only.
    /// </summary>
    Task<bool> UpdateStatusAsync(int id, string status);

    /// <summary>
    /// Soft delete device.
    /// </summary>
    Task<bool> DeleteAsync(int id);

    /// <summary>
    /// Get paged devices without summary.
    /// </summary>
    Task<PagedResultDto<DeviceDto>> GetPagedAsync(
        int page,
        int pageSize,
        int? accountId = null,
        string? search = null);

    /// <summary>
    /// Bulk create devices.
    /// </summary>
    Task<List<DeviceDto>> BulkCreateAsync(List<CreateDeviceDto> devices);

    /// <summary>
    /// Export devices as CSV.
    /// </summary>
    Task<byte[]> ExportdeviceCsvAsync(int? accountId, string? search);
    Task<byte[]> ExportDevicesXlsxAsync(int? accountId, string? search);
}
