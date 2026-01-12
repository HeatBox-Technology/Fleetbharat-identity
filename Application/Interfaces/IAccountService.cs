using System.Collections.Generic;
using System.Threading.Tasks;
public interface IAccountService
{
    Task<mst_account> CreateAsync(mst_account account);
    Task<List<mst_account>> GetAllAsync();
    Task<mst_account> GetByIdAsync(int id);
    Task UpdateAsync(int id, mst_account account);
    Task UpdateStatusAsync(int id, string status);
    Task DeleteAsync(int id);
}
