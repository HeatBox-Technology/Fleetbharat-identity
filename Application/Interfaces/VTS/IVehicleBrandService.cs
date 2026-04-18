using System.Threading.Tasks;

public interface IVehicleBrandService
{
    Task<int> CreateAsync(VehicleBrandDto dto);

    Task<VehicleBrandListUiResponseDto> GetBrands(
        int page,
        int pageSize,
        string? search);

    Task<VehicleBrandDto?> GetByIdAsync(int id);

    Task<bool> UpdateAsync(int id, VehicleBrandDto dto);

    Task<bool> UpdateStatusAsync(int id, bool isEnabled);

    Task<bool> DeleteAsync(int id);
}
