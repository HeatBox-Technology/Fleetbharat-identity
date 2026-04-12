using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public class RoleService : IRoleService
{
    private readonly IdentityDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public RoleService(IdentityDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    // -----------------------------
    // CREATE ROLE
    // -----------------------------
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
            UpdatedOn = DateTime.UtcNow,
            CreatedBy = _currentUser.AccountId, // Assuming creator is from the same account
        };

        _db.Roles.Add(role);
        await _db.SaveChangesAsync();

        if (req.Rights?.Any() == true)
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
            });

            _db.FormRoleRights.AddRange(rights);
            await _db.SaveChangesAsync();
        }

        return role.RoleId;
    }

    // -----------------------------
    // GET ROLES BY ACCOUNT
    // -----------------------------
    public async Task<List<mst_role>> GetByAccountAsync(int accountId)
    {
        return await _db.Roles
            .AsNoTracking()
            .ApplyAccountHierarchyFilter(_currentUser)
            .Where(x => x.AccountId == accountId && x.IsActive && !x.IsDeleted)
            .OrderBy(x => x.RoleName)
            .ToListAsync();
    }

    // -----------------------------
    // GET ROLE RIGHTS
    // -----------------------------
    public async Task<List<FormRightResponseDto>> GetRoleRightsAsync(int roleId)
    {
        var role = await _db.Roles
            .AsNoTracking()
            .ApplyAccountHierarchyFilter(_currentUser)
            .FirstOrDefaultAsync(x => x.RoleId == roleId);

        if (role == null)
            return new List<FormRightResponseDto>();

        if (IsSystemRole(role))
            return await BuildFullAccessRightsAsync();

        return await (
            from r in _db.FormRoleRights
            join f in _db.Forms on r.FormId equals f.FormId
            where r.RoleId == roleId
            select new FormRightResponseDto
            {
                FormId = f.FormId,
                FormCode = f.FormCode,
                FormName = f.FormName,
                PageUrl = f.PageUrl,
                icon = f.IconName,
                CanRead = r.CanRead,
                CanWrite = r.CanWrite,
                CanUpdate = r.CanUpdate,
                CanDelete = r.CanDelete,
                CanExport = r.CanExport,
                CanAll = r.CanAll
            }).ToListAsync();
    }

    // -----------------------------
    // UPDATE RIGHTS
    // -----------------------------
    public async Task<bool> UpdateRightsAsync(int roleId, List<RoleFormRightDto> rights)
    {
        var role = await _db.Roles
            .ApplyAccountHierarchyFilter(_currentUser)
            .FirstOrDefaultAsync(x => x.RoleId == roleId);

        if (role == null) return false;

        var existing = await _db.FormRoleRights
            .Where(x => x.RoleId == roleId)
            .ToListAsync();

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
        });

        _db.FormRoleRights.AddRange(newRights);
        await _db.SaveChangesAsync();

        return true;
    }

    // -----------------------------
    // UPDATE ROLE
    // -----------------------------
    public async Task<bool> UpdateAsync(int roleId, UpdateRoleRequest req)
    {
        var role = await _db.Roles
            .ApplyAccountHierarchyFilter(_currentUser)
            .FirstOrDefaultAsync(x => x.RoleId == roleId);

        if (role == null) return false;

        role.RoleName = req.RoleName.Trim();
        role.Description = req.Description?.Trim();
        role.RoleCode = req.RoleCode.Trim();
        role.IsActive = req.IsActive;
        role.UpdatedOn = DateTime.UtcNow;
        role.UpdatedBy = _currentUser.AccountId;

        await _db.SaveChangesAsync();
        return true;
    }

    // -----------------------------
    // DELETE ROLE
    // -----------------------------
    public async Task<bool> DeleteAsync(int roleId)
    {
        var role = await _db.Roles
            .ApplyAccountHierarchyFilter(_currentUser)
            .FirstOrDefaultAsync(x => x.RoleId == roleId && !x.IsDeleted);

        if (role == null) return false;

        // ✅ Soft delete role
        role.IsDeleted = true;
        role.DeletedOn = DateTime.UtcNow;
        role.DeletedBy = _currentUser.AccountId;

        // ✅ Soft delete related rights (recommended)
        var rights = await _db.FormRoleRights
            .Where(x => x.RoleId == roleId)
            .ToListAsync();
        _db.FormRoleRights.RemoveRange(rights);

        await _db.SaveChangesAsync();
        return true;
    }

    // -----------------------------
    // GET ROLES LIST (GRID + CARDS)
    // -----------------------------
    public async Task<RoleListUiResponseDto> GetRoles(
        int page,
        int pageSize,
        int? accountId,
        string? search)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 10;

        var roleQuery = _db.Roles
            .AsNoTracking()
            .ApplyAccountHierarchyFilter(_currentUser)
            .Where(x => !x.IsDeleted)
            .AsQueryable();

        if (accountId.HasValue)
            roleQuery = roleQuery.Where(x => x.AccountId == accountId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            roleQuery = roleQuery.Where(x =>
                (x.RoleName != null && x.RoleName.ToLower().Contains(s)) ||
                (x.Description != null && x.Description.ToLower().Contains(s)));
        }

        var summaryData = await roleQuery
            .GroupBy(x => 1)
            .Select(g => new
            {
                Total = g.Count(),
                SystemRoles = g.Count(x => x.IsSystemRole)
            })
            .FirstOrDefaultAsync();

        var totalRoles = summaryData?.Total ?? 0;
        var systemRoles = summaryData?.SystemRoles ?? 0;

        var summary = new RoleCardSummaryDto
        {
            TotalRoles = totalRoles,
            SystemRoles = systemRoles,
            CustomRoles = totalRoles - systemRoles
        };

        var tableQuery =
            from r in roleQuery
            join a in _db.Accounts.AsNoTracking()
                .ApplyAccountHierarchyFilter(_currentUser)
                on r.AccountId equals a.AccountId
            select new
            {
                Role = r,
                AccountName = a.AccountName
            };

        var totalRecords = await tableQuery.CountAsync();

        var items = await tableQuery
            .OrderByDescending(x => x.Role.UpdatedOn ?? x.Role.CreatedOn)
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

    // -----------------------------
    // GET ROLE DETAIL (FIXED)
    // -----------------------------
    public async Task<RoleDetailResponseDto?> GetByRoleIdAsync(int roleId, int? accountId)
    {
        var role = await _db.Roles
            .AsNoTracking()
            .ApplyAccountHierarchyFilter(_currentUser)
            .FirstOrDefaultAsync(x => x.RoleId == roleId);

        if (role == null) return null;

        var rights = IsSystemRole(role)
            ? await BuildFullAccessRightsAsync()
            : await (
                from rr in _db.FormRoleRights.AsNoTracking()
                join f in _db.Forms.AsNoTracking() on rr.FormId equals f.FormId
                where rr.RoleId == roleId
                orderby f.SortOrder
                select new FormRightResponseDto
                {
                    FormId = f.FormId,
                    FormCode = f.FormCode,
                    FormName = f.FormName,
                    PageUrl = f.PageUrl,
                    icon = f.IconName,
                    CanRead = rr.CanRead,
                    CanWrite = rr.CanWrite,
                    CanUpdate = rr.CanUpdate,
                    CanDelete = rr.CanDelete,
                    CanExport = rr.CanExport,
                    CanAll = rr.CanAll
                }).ToListAsync();

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

    // -----------------------------
    // HELPERS
    // -----------------------------
    private static bool IsSystemRole(mst_role role) =>
        role.IsSystemRole ||
        role.RoleName.Equals("System", StringComparison.OrdinalIgnoreCase);

    private Task<List<FormRightResponseDto>> BuildFullAccessRightsAsync() =>
        _db.Forms.AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .Select(f => new FormRightResponseDto
            {
                FormId = f.FormId,
                FormCode = f.FormCode,
                FormName = f.FormName,
                PageUrl = f.PageUrl,
                icon = f.IconName,
                CanRead = true,
                CanWrite = true,
                CanUpdate = true,
                CanDelete = true,
                CanExport = true,
                CanAll = true
            })
            .ToListAsync();

    public async Task<byte[]> ExportRolesCsvAsync(int? accountId, string? search)
    {
        // ✅ Base role query (exclude deleted + apply hierarchy)
        var roleQuery = _db.Roles
            .AsNoTracking()
            .Where(r => !r.IsDeleted)
            .ApplyAccountHierarchyFilter(_currentUser)
            .AsQueryable();

        // ✅ Apply account filter correctly
        if (accountId.HasValue)
            roleQuery = roleQuery.Where(r => r.AccountId == accountId.Value);

        // ✅ Apply search filter
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();

            roleQuery = roleQuery.Where(r =>
                r.RoleName.ToLower().Contains(s) ||
                (r.Description != null && r.Description.ToLower().Contains(s))
            );
        }

        // ✅ Join with accounts (exclude deleted)
        var query =
            from r in roleQuery
            join a in _db.Accounts
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .ApplyAccountHierarchyFilter(_currentUser)
            on r.AccountId equals a.AccountId
            select new
            {
                a.AccountName,
                r.RoleName,
                r.Description,
                r.IsSystemRole,
                r.CreatedOn,

                // ✅ Correct AssignedUsers count
                AssignedUsers = _db.Users
                    .Where(u =>
                        !u.IsDeleted &&
                        u.roleId == r.RoleId
                    )
                    .Count()
            };

        var rows = await query.ToListAsync();

        // ✅ CSV Build
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Account,RoleName,Description,AssignedUsers,IsSystemRole,CreatedOn");

        foreach (var r in rows)
        {
            sb.AppendLine(
                $"{r.AccountName}," +
                $"{r.RoleName}," +
                $"{r.Description}," +
                $"{r.AssignedUsers}," +
                $"{r.IsSystemRole}," +
                $"{r.CreatedOn:yyyy-MM-dd}"
            );
        }

        return System.Text.Encoding.UTF8.GetBytes(sb.ToString());
    }

    public async Task<byte[]> ExportRolesXlsxAsync(int? accountId, string? search)
    {
        // ✅ Base role query (exclude deleted + apply hierarchy)
        var roleQuery = _db.Roles
            .AsNoTracking()
            .Where(r => !r.IsDeleted)
            .ApplyAccountHierarchyFilter(_currentUser)
            .AsQueryable();

        // ✅ Apply account filter correctly
        if (accountId.HasValue)
            roleQuery = roleQuery.Where(r => r.AccountId == accountId.Value);

        // ✅ Apply search filter
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();

            roleQuery = roleQuery.Where(r =>
                r.RoleName.ToLower().Contains(s) ||
                (r.Description != null && r.Description.ToLower().Contains(s))
            );
        }

        // ✅ Join with accounts (exclude deleted)
        var query =
            from r in roleQuery
            join a in _db.Accounts
                .AsNoTracking()
                .Where(x => !x.IsDeleted)
                .ApplyAccountHierarchyFilter(_currentUser)
            on r.AccountId equals a.AccountId
            select new
            {
                a.AccountName,
                r.RoleName,
                r.Description,
                r.IsSystemRole,
                r.CreatedOn,

                // ✅ Correct AssignedUsers count
                AssignedUsers = _db.Users
                    .Where(u =>
                        !u.IsDeleted &&
                        u.roleId == r.RoleId
                    )
                    .Count()
            };

        var rows = await query.ToListAsync();

        using (var workbook = new ClosedXML.Excel.XLWorkbook())
        {
            var worksheet = workbook.Worksheets.Add("Roles");

            // Add headers
            worksheet.Cell(1, 1).Value = "Account";
            worksheet.Cell(1, 2).Value = "Role Name";
            worksheet.Cell(1, 3).Value = "Description";
            worksheet.Cell(1, 4).Value = "Assigned Users";
            worksheet.Cell(1, 5).Value = "Is System Role";
            worksheet.Cell(1, 6).Value = "Created On";

            // Style header row
            var headerRow = worksheet.Row(1);
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;

            // Add data
            int rowNumber = 2;
            foreach (var item in rows)
            {
                worksheet.Cell(rowNumber, 1).Value = item.AccountName;
                worksheet.Cell(rowNumber, 2).Value = item.RoleName;
                worksheet.Cell(rowNumber, 3).Value = item.Description;
                worksheet.Cell(rowNumber, 4).Value = item.AssignedUsers;
                worksheet.Cell(rowNumber, 5).Value = item.IsSystemRole ? "Yes" : "No";
                worksheet.Cell(rowNumber, 6).Value = item.CreatedOn.ToString("yyyy-MM-dd");
                rowNumber++;
            }

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();

            // Return as bytes
            using (var stream = new System.IO.MemoryStream())
            {
                workbook.SaveAs(stream);
                return stream.ToArray();
            }
        }
    }
}
