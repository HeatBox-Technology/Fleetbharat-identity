using System.Threading.Tasks;

public interface IDriverVehicleAssignmentService
{
    Task<int> CreateAsync(CreateDriverVehicleAssignmentDto dto);
    Task<bool> UpdateAsync(int id, UpdateDriverVehicleAssignmentDto dto);
    Task<PagedResultDto<DriverVehicleAssignmentDto>> GetAllAsync(
        int page,
        int pageSize,
        int? accountContextId,
        string? search);
    Task<DriverVehicleAssignmentDto?> GetByIdAsync(int id);
    Task<bool> DeleteAsync(int id);
    Task<bool> SoftDeleteAsync(int id);
}
