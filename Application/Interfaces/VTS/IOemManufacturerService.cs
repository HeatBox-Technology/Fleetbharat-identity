using System.Threading.Tasks;

public interface IOemManufacturerService
{
    Task<int> CreateAsync(OemManufacturerDto dto);

    Task<OemListUiResponseDto> GetManufacturers(
        int page,
        int pageSize,
        string? search);

    Task<OemManufacturerDto?> GetByIdAsync(int id);

    Task<bool> UpdateAsync(int id, OemManufacturerDto dto);

    Task<bool> UpdateStatusAsync(int id, bool isEnabled);

    Task<bool> DeleteAsync(int id);
}