using System.Collections.Generic;
using System.Threading.Tasks;

public interface IFormService
{
    Task<mst_form> CreateAsync(mst_form form);
    Task<List<mst_form>> GetAllAsync();
    Task<mst_form?> GetByIdAsync(int id);
    Task UpdateAsync(int id, mst_form form);
    Task UpdateStatusAsync(int id, bool isActive);
    Task DeleteAsync(int id);
}
