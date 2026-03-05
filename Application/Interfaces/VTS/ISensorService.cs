using Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;


public interface ISensorService
{
    Task<IEnumerable<SensorDto>> GetAllAsync(int page = 1, int pageSize = 10, string? search = null);
    Task<SensorDto?> GetByIdAsync(long id);
    Task<SensorDto> CreateAsync(SensorDto dto);
    Task<SensorDto> UpdateAsync(long id, SensorDto dto);
    Task<bool> DeleteAsync(long id);
}

