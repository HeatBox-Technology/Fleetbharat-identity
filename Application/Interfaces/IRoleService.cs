using System.Collections.Generic;
using System.Threading.Tasks;

public interface IRoleService
{
    Task<mst_role> CreateAsync(mst_role role);
    Task<List<mst_role>> GetAllAsync();
    Task<mst_role?> GetByIdAsync(int id);
    Task UpdateAsync(int id, mst_role role);
    Task UpdateStatusAsync(int id, bool isActive);
    Task DeleteAsync(int id);
}