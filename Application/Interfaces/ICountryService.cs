using System.Collections.Generic;
using System.Threading.Tasks;
public interface ICountryService
{
    Task<mst_country> CreateAsync(mst_country country);
    Task<List<mst_country>> GetAllAsync();
    Task<mst_country?> GetByIdAsync(int id);
    Task UpdateAsync(int id, mst_country country);
    Task UpdateStatusAsync(int id, bool isActive);
    Task DeleteAsync(int id);
}
