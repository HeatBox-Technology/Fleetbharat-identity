using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public class RoleService : IRoleService
{
    private readonly IdentityDbContext _db;

    public RoleService(IdentityDbContext db)
    {
        _db = db;
    }

    public async Task<int> CreateAsync(CreateRoleRequest req)
    {
        var role = new mst_role
        {
            AccountId = req.AccountId,
            RoleName = req.RoleName.Trim(),
            Description = req.Description?.Trim(),
            IsActive = true,
            CreatedOn = DateTime.UtcNow,
            UpdatedOn = DateTime.UtcNow
        };

        _db.Roles.Add(role);
        await _db.SaveChangesAsync();

        if (req.Rights.Any())
        {
            var rights = req.Rights.Select(x => new map_FormRole_right
            {
                RoleId = role.RoleId,
                FormId = x.FormId,
                CanRead = x.CanRead,
                CanWrite = x.CanWrite,
                CanDelete = x.CanDelete,
                CanExport = x.CanExport,
                CanAll = x.CanAll
            }).ToList();

            _db.FormRoleRights.AddRange(rights);
            await _db.SaveChangesAsync();
        }

        return role.RoleId;
    }

    public async Task<List<mst_role>> GetByAccountAsync(int accountId)
    {
        return await _db.Roles
            .Where(x => x.AccountId == accountId && x.IsActive)
            .OrderBy(x => x.RoleName)
            .ToListAsync();
    }

    public async Task<List<FormRightResponseDto>> GetRoleRightsAsync(int roleId)
    {
        return await (from r in _db.FormRoleRights
                      join f in _db.Forms on r.FormId equals f.FormId
                      where r.RoleId == roleId
                      select new FormRightResponseDto
                      {
                          FormId = f.FormId,
                          FormCode = f.FormCode,
                          FormName = f.FormName,
                          CanRead = r.CanRead,
                          CanWrite = r.CanWrite,
                          CanDelete = r.CanDelete,
                          CanExport = r.CanExport,
                          CanAll = r.CanAll
                      }).ToListAsync();
    }

    public async Task<bool> UpdateRightsAsync(int roleId, List<RoleFormRightDto> rights)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(x => x.RoleId == roleId);
        if (role == null) return false;

        var existing = await _db.FormRoleRights.Where(x => x.RoleId == roleId).ToListAsync();
        _db.FormRoleRights.RemoveRange(existing);

        var newRights = rights.Select(x => new map_FormRole_right
        {
            RoleId = roleId,
            FormId = x.FormId,
            CanRead = x.CanRead,
            CanWrite = x.CanWrite,
            CanDelete = x.CanDelete,
            CanExport = x.CanExport,
            CanAll = x.CanAll
        }).ToList();

        _db.FormRoleRights.AddRange(newRights);
        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<bool> UpdateAsync(int roleId, UpdateRoleRequest req)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(x => x.RoleId == roleId);
        if (role == null) return false;

        role.RoleName = req.RoleName.Trim();
        role.Description = req.Description?.Trim();
        role.IsActive = req.IsActive;
        role.UpdatedOn = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int roleId)
    {
        var role = await _db.Roles.FirstOrDefaultAsync(x => x.RoleId == roleId);
        if (role == null) return false;

        var rights = await _db.FormRoleRights.Where(x => x.RoleId == roleId).ToListAsync();
        _db.FormRoleRights.RemoveRange(rights);

        _db.Roles.Remove(role);
        await _db.SaveChangesAsync();
        return true;
    }
}
