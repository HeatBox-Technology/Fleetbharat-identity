using System.Threading.Tasks;

public interface IVehicleAccessService
{
    Task<long> CreateAsync(CreateVehicleAccessRequest request);
    Task<bool> UpdateAsync(long id, UpdateVehicleAccessRequest request);
    Task<VehicleAccessListResponse> GetAllAsync(int? accountId, string? search, int pageNumber, int pageSize);
    Task<VehicleAccessResponse?> GetByIdAsync(long id);
    Task<bool> DeleteAsync(long id, DeleteVehicleAccessRequest request);
    Task<byte[]> ExportCsvAsync(int? accountId, string? search);
}
