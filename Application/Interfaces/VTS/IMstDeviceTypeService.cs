using System.Collections.Generic;
using System.Threading.Tasks;

public interface IMstDeviceTypeService
{
    Task<int> CreateAsync(CreateMstDeviceTypeRequestDto request);
    Task<List<MstDeviceTypeResponseDto>> GetAllAsync(string? search = null);
    Task<MstDeviceTypeResponseDto?> GetByIdAsync(int id);
    Task<bool> UpdateAsync(int id, UpdateMstDeviceTypeRequestDto request);
    Task<bool> DeleteAsync(int id);
}
