using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public class FormRoleRightService : IFormRoleRightService
{
    private readonly IdentityDbContext _context;

    public FormRoleRightService(IdentityDbContext context)
    {
        _context = context;
    }

    public async Task<map_FormRole_right> CreateAsync(map_FormRole_right right)
    {
        _context.FormRoleRights.Add(right);
        await _context.SaveChangesAsync();
        return right;
    }

    public async Task<List<map_FormRole_right>> GetAllAsync(int page = 1, int pageSize = 10, string? search = null)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 10;

        var query = _context.FormRoleRights.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(x =>
                x.RoleId.ToString().ToLower().Contains(s) ||
                x.FormId.ToString().ToLower().Contains(s));
        }

        return await query
            .OrderByDescending(x => x.RoleFormRightId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new map_FormRole_right
            {
                RoleFormRightId = x.RoleFormRightId,
                RoleId = x.RoleId,
                FormId = x.FormId,
                CanRead = x.CanRead,
                CanWrite = x.CanWrite,
                CanUpdate = x.CanUpdate,
                CanDelete = x.CanDelete,
                CanExport = x.CanExport,
                CanAll = x.CanAll
            })
            .ToListAsync();
    }

    public async Task<List<map_FormRole_right>> GetByRoleAsync(int roleId)
    {
        return await _context.FormRoleRights
            .Where(x => x.RoleId == roleId)
            .ToListAsync();
    }

    public async Task<map_FormRole_right?> GetByIdAsync(int id)
    {
        return await _context.FormRoleRights.FindAsync(id);
    }

    public async Task UpdateAsync(int id, map_FormRole_right right)
    {
        var existing = await _context.FormRoleRights.FindAsync(id);
        if (existing == null) return;

        existing.RoleId = right.RoleId;
        existing.FormId = right.FormId;
        existing.CanRead = right.CanRead;
        existing.CanWrite = right.CanWrite;
        existing.CanDelete = right.CanDelete;
        existing.CanExport = right.CanExport;
        existing.CanAll = right.CanAll;

        await _context.SaveChangesAsync();
    }

    // PATCH-style: update permissions only
    public async Task UpdateRightsAsync(int id, map_FormRole_right right)
    {
        var existing = await _context.FormRoleRights.FindAsync(id);
        if (existing == null) return;

        existing.CanRead = right.CanRead;
        existing.CanWrite = right.CanWrite;
        existing.CanDelete = right.CanDelete;
        existing.CanExport = right.CanExport;
        existing.CanAll = right.CanAll;

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var right = await _context.FormRoleRights.FindAsync(id);
        if (right == null) return;

        _context.FormRoleRights.Remove(right);
        await _context.SaveChangesAsync();
    }
}
