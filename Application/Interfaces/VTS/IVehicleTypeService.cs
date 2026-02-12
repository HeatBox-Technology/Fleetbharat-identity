using Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IVehicleTypeService
{
    Task<IEnumerable<VehicleTypeDto>> GetAllAsync();
    Task<VehicleTypeDto?> GetByIdAsync(int id);
    Task<VehicleTypeDto> CreateAsync(VehicleTypeDto dto);
    Task<VehicleTypeDto> UpdateAsync(int id, VehicleTypeDto dto);
    Task<bool> DeleteAsync(int id);
}

