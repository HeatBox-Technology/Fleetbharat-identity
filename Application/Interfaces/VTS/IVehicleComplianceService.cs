using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

public interface IVehicleComplianceService
{
    Task<int> CreateAsync(CreateVehicleComplianceDto dto, IFormFile? document = null);

    Task<VehicleComplianceListUiResponseDto> GetDocuments(
        int page,
        int pageSize,
        int? accountId,
        int? vehicleId,
        string? complianceType,
        string? status,
        string? search);

    Task<VehicleComplianceDto?> GetByIdAsync(int id);

    Task<bool> UpdateAsync(int id, UpdateVehicleComplianceDto dto, IFormFile? document = null);

    Task<bool> DeleteAsync(int id);
}
