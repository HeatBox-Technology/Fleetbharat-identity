using System.Collections.Generic;
using System.Threading.Tasks;
public interface IStateService
{
    Task<mst_state> CreateAsync(mst_state state);
    Task<List<mst_state>> GetAllAsync();
    Task<List<mst_state>> GetByCountryAsync(int countryId);
    Task<mst_state?> GetByIdAsync(int id);
    Task UpdateAsync(int id, mst_state state);
    Task UpdateStatusAsync(int id, bool isActive);
    Task DeleteAsync(int id);
}
