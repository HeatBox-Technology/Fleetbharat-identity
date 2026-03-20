using System.Collections.Generic;
using System.Threading.Tasks;
public interface ICityService
{
    Task<mst_city> CreateAsync(mst_city city);
    Task<List<mst_city>> GetAllAsync(int page = 1, int pageSize = 10, string? search = null);
    Task<List<mst_city>> GetByStateAsync(int stateId);
    Task<mst_city?> GetByIdAsync(int id);
    Task UpdateAsync(int id, mst_city city);
    Task UpdateStatusAsync(int id, bool isActive);
    Task DeleteAsync(int id);
}
