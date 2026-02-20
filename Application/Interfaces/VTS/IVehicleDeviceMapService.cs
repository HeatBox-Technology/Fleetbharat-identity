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
    Task<int> CreateAsync(VehicleDeviceMapDto dto);

    /// <summary>
    /// Get mappings with pagination and dashboard summary.
    /// </summary>
    Task<VehicleDeviceAssignmentListUiResponseDto> GetAssignments(
        int page,
        int pageSize,
        long? accountId,
        string? search);

    /// <summary>
    /// Get mapping by Id.
    /// </summary>
    Task<VehicleDeviceMapDto?> GetByIdAsync(int id);

    /// <summary>
    /// Update mapping.
    /// </summary>
    Task<bool> UpdateAsync(int id, VehicleDeviceMapDto dto);

    /// <summary>
    /// Update active status.
    /// </summary>
    Task<bool> UpdateStatusAsync(int id, int isActive);

    /// <summary>
    /// Soft delete mapping.
    /// </summary>
    Task<bool> DeleteAsync(int id);

    /// <summary>
    /// Bulk create mappings.
    /// </summary>
    Task<List<VehicleDeviceMapDto>> BulkCreateAsync(List<VehicleDeviceMapDto> items);
}