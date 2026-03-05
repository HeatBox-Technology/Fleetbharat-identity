using System.Collections.Generic;
using System.Threading.Tasks;

public interface IFormRoleRightService
{
    Task<map_FormRole_right> CreateAsync(map_FormRole_right right);
    Task<List<map_FormRole_right>> GetAllAsync(int page = 1, int pageSize = 10, string? search = null);
    Task<List<map_FormRole_right>> GetByRoleAsync(int roleId);
    Task<map_FormRole_right?> GetByIdAsync(int id);
    Task UpdateAsync(int id, map_FormRole_right right);
    Task UpdateRightsAsync(int id, map_FormRole_right right);
    Task DeleteAsync(int id);
}
