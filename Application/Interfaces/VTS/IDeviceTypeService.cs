using System.Threading.Tasks;

public interface IDeviceTypeService
{
    Task<int> CreateAsync(DeviceTypeDto dto);

    Task<DeviceTypeListUiResponseDto> GetDeviceTypes(
        int page,
        int pageSize,
        string? search);

    Task<DeviceTypeDto?> GetByIdAsync(int id);

    Task<bool> UpdateAsync(int id, DeviceTypeDto dto);

    Task<bool> UpdateStatusAsync(int id, bool isEnabled);

    Task<bool> DeleteAsync(int id);
}