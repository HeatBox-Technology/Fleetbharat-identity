using Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;


public interface IDriverService
{
    Task<PagedResultDto<DriverDto>> GetPagedAsync(int page, int pageSize);
    Task<IEnumerable<DriverDto>> GetAllAsync();
    Task<DriverDto> GetByIdAsync(long driverId);
    Task<DriverDto> CreateAsync(DriverDto dto);
    Task<DriverDto> UpdateAsync(DriverDto dto);
    Task<bool> DeleteAsync(long driverId);
    Task<IEnumerable<DriverDto>> BulkCreateAsync(IEnumerable<DriverDto> drivers);
}

