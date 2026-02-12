using Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;


public interface ISensorService
{
    Task<IEnumerable<SensorDto>> GetAllAsync();
    Task<SensorDto?> GetByIdAsync(long id);
    Task<SensorDto> CreateAsync(SensorDto dto);
    Task<SensorDto> UpdateAsync(long id, SensorDto dto);
    Task<bool> DeleteAsync(long id);
}

