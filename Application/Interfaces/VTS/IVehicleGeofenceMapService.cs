using System.Threading.Tasks;

public interface IVehicleGeofenceMapService
{
    Task<int> CreateAsync(CreateVehicleGeofenceMapDto dto);
    Task<bool> UpdateAsync(int id, UpdateVehicleGeofenceMapDto dto);
    Task<bool> UpdateStatusAsync(int id, bool isActive);
    Task<bool> DeleteAsync(int id);
    Task<VehicleGeofenceMapDto?> GetByIdAsync(int id);

    Task<VehicleGeofenceAssignmentListUiResponseDto> GetAssignments(
        int page,
        int pageSize,
        int? accountId,
        string? search);
    Task<PagedResultDto<VehicleGeofenceMapDto>> GetPagedAsync(
  int page, int pageSize, int? accountId, string? search);
}