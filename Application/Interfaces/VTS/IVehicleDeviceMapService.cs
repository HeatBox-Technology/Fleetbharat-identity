using Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// Service for mapping vehicles to devices.
/// Provides CRUD operations and listing with summary.
/// </summary>
public interface IVehicleDeviceMapService
{
    /// <summary>
    /// Create a new vehicle-device mapping.
    /// </summary>
    Task<int> CreateAsync(CreateVehicleDeviceMapDto dto);

    /// <summary>
    /// Get mappings with pagination and dashboard summary.
    /// </summary>
    Task<VehicleDeviceAssignmentListUiResponseDto> GetAssignments(
        int page,
        int pageSize,
        int? accountId = null,
        string? search = null);

    /// <summary>
    /// Get mapping by Id.
    /// </summary>
    Task<VehicleDeviceMapDto?> GetByIdAsync(int id);

    /// <summary>
    /// Update mapping.
    /// </summary>
    Task<bool> UpdateAsync(int id, UpdateVehicleDeviceMapDto dto);

    /// <summary>
    /// Update active status.
    /// </summary>
    Task<bool> UpdateStatusAsync(int id, bool isActive);

    /// <summary>
    /// Soft delete mapping.
    /// </summary>
    Task<bool> DeleteAsync(int id);

    /// <summary>
    /// Get paged assignments without summary.
    /// </summary>
    Task<PagedResultDto<VehicleDeviceMapDto>> GetPagedAsync(
        int page,
        int pageSize,
        int? accountId = null,
        string? search = null);

    /// <summary>
    /// Bulk create mappings.
    /// </summary>
    Task<List<VehicleDeviceMapDto>> BulkCreateAsync(List<CreateVehicleDeviceMapDto> items);
}