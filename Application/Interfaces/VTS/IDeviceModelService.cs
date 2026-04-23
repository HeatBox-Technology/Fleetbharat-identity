using System.Threading.Tasks;

public interface IDeviceModelService
{
    Task<int> CreateAsync(CreateDeviceModelDto dto);

    Task<DeviceModelListUiResponseDto> GetModels(
        int page,
        int pageSize,
        string? search);

    Task<DeviceModelDto?> GetByIdAsync(int id);

    Task<bool> UpdateAsync(int id, UpdateDeviceModelDto dto);

    Task<bool> UpdateStatusAsync(int id, bool isEnabled);

    Task<bool> DeleteAsync(int id);
}
