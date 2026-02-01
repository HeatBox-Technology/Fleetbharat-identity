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
            RoleCode = req.RoleCode.Trim(),
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
                CanUpdate = x.CanUpdate,
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
                          CanUpdate = r.CanUpdate,
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
            CanUpdate = x.CanUpdate,
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
        role.RoleCode = req.RoleCode.Trim();
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

    public async Task<RoleListUiResponseDto> GetRoles(
        int page,
        int pageSize,
        int? accountId,
        string? search)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 10;

        var roleQuery = _db.Roles.AsNoTracking().AsQueryable();

        if (accountId.HasValue)
            roleQuery = roleQuery.Where(x => x.AccountId == accountId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            roleQuery = roleQuery.Where(x =>
                x.RoleName.ToLower().Contains(s) ||
                (x.Description != null && x.Description.ToLower().Contains(s)));
        }

        // ✅ Cards count (Summary)
        var totalRoles = await roleQuery.CountAsync();
        var systemRoles = await roleQuery.CountAsync(x => x.IsSystemRole);
        var customRoles = totalRoles - systemRoles;

        var summary = new RoleCardSummaryDto
        {
            TotalRoles = totalRoles,
            SystemRoles = systemRoles,
            CustomRoles = customRoles
        };

        // ✅ Table data + AssignedUsers Count + AccountName
        var tableQuery =
            from r in roleQuery
            join a in _db.Accounts.AsNoTracking() on r.AccountId equals a.AccountId
            select new
            {
                Role = r,
                AccountName = a.AccountName
            };

        var totalRecords = await tableQuery.CountAsync();

        var items = await tableQuery
            .OrderByDescending(x => x.Role.UpdatedOn)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new RoleListItemDto
            {
                RoleId = x.Role.RoleId,
                AccountId = x.Role.AccountId,
                AccountName = x.AccountName,

                RoleName = x.Role.RoleName,
                Description = x.Role.Description,
                IsSystemRole = x.Role.IsSystemRole,

                AssignedUsers = _db.Users.Count(u => u.roleId == x.Role.RoleId && !u.IsDeleted),
                CreatedOn = x.Role.CreatedOn
            })
            .ToListAsync();

        return new RoleListUiResponseDto
        {
            Summary = summary,
            Roles = new PagedResultDto<RoleListItemDto>
            {
                Page = page,
                PageSize = pageSize,
                TotalRecords = totalRecords,
                Items = items
            }
        };
    }
    public async Task<RoleDetailResponseDto?> GetByRoleIdAsync(int roleId, int accountId)
    {
        var role = await _db.Roles.AsNoTracking()
            .FirstOrDefaultAsync(x => x.RoleId == roleId && x.AccountId == accountId);

        if (role == null) return null;

        var rights = await (
            from rr in _db.FormRoleRights.AsNoTracking()
            join f in _db.Forms.AsNoTracking() on rr.FormId equals f.FormId
            where rr.RoleId == roleId
            orderby f.SortOrder
            select new FormRightResponseDto
            {
                FormId = f.FormId,
                FormCode = f.FormCode,
                FormName = f.FormName,
                PageUrl = f.PageUrl,// ✅ count
                icon = f.IconName,// ✅ coun
                CanRead = rr.CanRead,
                CanWrite = rr.CanWrite,
                CanUpdate = rr.CanUpdate,
                CanDelete = rr.CanDelete,
                CanExport = rr.CanExport,
                CanAll = rr.CanAll
            }
        ).ToListAsync();

        return new RoleDetailResponseDto
        {
            RoleId = role.RoleId,
            AccountId = role.AccountId,
            RoleName = role.RoleName,
            Description = role.Description,
            IsActive = role.IsActive,
            IsSystemRole = role.IsSystemRole,
            CreatedOn = role.CreatedOn,
            UpdatedOn = role.UpdatedOn,
            Rights = rights
        };
    }

    public async Task<byte[]> ExportRolesCsvAsync(int? accountId, string? search)
    {
        var query =
            from r in _db.Roles.AsNoTracking()
            join a in _db.Accounts.AsNoTracking() on r.AccountId equals a.AccountId
            select new
            {
                a.AccountName,
                r.RoleName,
                r.Description,
                r.IsSystemRole,
                r.CreatedOn,
                AssignedUsers = _db.Users.Count(u => u.roleId == r.RoleId && !u.IsDeleted)
            };

        if (accountId.HasValue)
            query = query.Where(x => x.AccountName != null && _db.Roles.Any(r => r.AccountId == accountId.Value));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(x =>
                x.RoleName.ToLower().Contains(s) ||
                (x.Description != null && x.Description.ToLower().Contains(s)) ||
                x.AccountName.ToLower().Contains(s));
        }

        var rows = await query.ToListAsync();

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Account,RoleName,Description,AssignedUsers,IsSystemRole,CreatedOn");

        foreach (var r in rows)
        {
            sb.AppendLine($"{r.AccountName},{r.RoleName},{r.Description},{r.AssignedUsers},{r.IsSystemRole},{r.CreatedOn:yyyy-MM-dd}");
        }

        return System.Text.Encoding.UTF8.GetBytes(sb.ToString());
    }


}
