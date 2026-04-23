using System.Threading.Tasks;

public interface IServiceVendorService
{
    Task<int> CreateAsync(ServiceVendorDto dto);

    Task<ServiceVendorListUiResponseDto> GetVendors(
        int page,
        int pageSize,
        string? search);

    Task<ServiceVendorDto?> GetByIdAsync(int id);

    Task<bool> UpdateAsync(int id, ServiceVendorDto dto);

    Task<bool> UpdateStatusAsync(int id, bool isEnabled);

    Task<bool> DeleteAsync(int id);
}
