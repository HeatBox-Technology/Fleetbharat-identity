using Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface ISensorTypeService
{
    Task<IEnumerable<SensorTypeDto>> GetAllAsync();
    Task<SensorTypeDto?> GetByIdAsync(long id);
    Task<SensorTypeDto> CreateAsync(SensorTypeDto dto);
    Task<SensorTypeDto> UpdateAsync(long id, SensorTypeDto dto);
    Task<bool> DeleteAsync(long id);
}

