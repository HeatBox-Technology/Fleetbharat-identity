using Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IVehicleTypeService
{
    Task<IEnumerable<VehicleTypeDto>> GetAllAsync(int page = 1, int pageSize = 10, string? search = null);
    Task<VehicleTypeDto?> GetByIdAsync(int id);
    Task<VehicleTypeDto> CreateAsync(VehicleTypeDto dto);
    Task<VehicleTypeDto> UpdateAsync(int id, VehicleTypeDto dto);
    Task<bool> DeleteAsync(int id);
}

