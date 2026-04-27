using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public class VehicleAccessService : IVehicleAccessService
{
    private readonly IdentityDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public VehicleAccessService(IdentityDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<long> CreateAsync(CreateVehicleAccessRequest request)
    {
        await ValidateCreateAsync(request);

        var now = DateTime.UtcNow;
        var formIds = NormalizeFormIds(request.FormIds);
        var userId = NormalizeGuidString(request.UserId);

        var entity = new VehicleAccess
        {
            AccountId = request.AccountId,
            UserId = userId,
            VehicleId = request.VehicleId,
            AccessStartDate = request.AccessStartDate,
            AccessEndDate = request.AccessEndDate,
            CanViewTracking = request.CanViewTracking,
            CanViewReports = request.CanViewReports,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = now,
            CreatedBy = request.CreatedBy.Trim()
        };

        foreach (var formId in formIds)
        {
            entity.Forms.Add(new VehicleAccessForm
            {
                FormId = formId,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = now,
                CreatedBy = request.CreatedBy.Trim()
            });
        }

        _db.VehicleAccesses.Add(entity);
        await _db.SaveChangesAsync();

        return entity.Id;
    }

    public async Task<bool> UpdateAsync(long id, UpdateVehicleAccessRequest request)
    {
        var entity = await _db.VehicleAccesses
            .Include(x => x.Forms)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (entity == null)
            return false;

        await ValidateUpdateAsync(id, entity.AccountId, request);

        var now = DateTime.UtcNow;
        var updatedBy = request.UpdatedBy.Trim();
        var formIds = NormalizeFormIds(request.FormIds);
        var userId = NormalizeGuidString(request.UserId);

        entity.UserId = userId;
        entity.VehicleId = request.VehicleId;
        entity.AccessStartDate = request.AccessStartDate;
        entity.AccessEndDate = request.AccessEndDate;
        entity.CanViewTracking = request.CanViewTracking;
        entity.CanViewReports = request.CanViewReports;
        entity.IsActive = request.IsActive;
        entity.UpdatedAt = now;
        entity.UpdatedBy = updatedBy;

        foreach (var mapping in entity.Forms.Where(x => !x.IsDeleted))
        {
            mapping.IsActive = false;
            mapping.IsDeleted = true;
            mapping.DeletedAt = now;
            mapping.DeletedBy = updatedBy;
            mapping.UpdatedAt = now;
            mapping.UpdatedBy = updatedBy;
        }

        foreach (var formId in formIds)
        {
            entity.Forms.Add(new VehicleAccessForm
            {
                VehicleAccessId = entity.Id,
                FormId = formId,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = now,
                CreatedBy = updatedBy
            });
        }

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<VehicleAccessListResponse> GetAllAsync(
        int? accountId,
        string? search,
        int pageNumber,
        int pageSize)
    {
        if (pageNumber <= 0) pageNumber = 1;
        if (pageSize <= 0) pageSize = 10;

        var query = BuildBaseQuery(accountId, search);
        var totalCount = await query.CountAsync();

        var rows = await query
            .OrderByDescending(x => x.Access.UpdatedAt ?? x.Access.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var ids = rows.Select(x => x.Access.Id).ToList();
        var formsByAccessId = await GetFormsByAccessIdAsync(ids);

        return new VehicleAccessListResponse
        {
            Items = rows.Select(x => MapResponse(x, formsByAccessId)).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<VehicleAccessResponse?> GetByIdAsync(long id)
    {
        var row = await BuildBaseQuery(null, null)
            .FirstOrDefaultAsync(x => x.Access.Id == id);

        if (row == null)
            return null;

        var formsByAccessId = await GetFormsByAccessIdAsync(new List<long> { id });
        return MapResponse(row, formsByAccessId);
    }

    public async Task<bool> DeleteAsync(long id, DeleteVehicleAccessRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.DeletedBy) || !Guid.TryParse(request.DeletedBy, out _))
            throw new ArgumentException("deletedBy is required and must be a valid GUID.");

        var entity = await _db.VehicleAccesses
            .Include(x => x.Forms)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (entity == null)
            return false;

        var now = DateTime.UtcNow;
        var deletedBy = request.DeletedBy.Trim();

        entity.IsDeleted = true;
        entity.IsActive = false;
        entity.DeletedAt = now;
        entity.DeletedBy = deletedBy;

        foreach (var mapping in entity.Forms.Where(x => !x.IsDeleted))
        {
            mapping.IsDeleted = true;
            mapping.IsActive = false;
            mapping.DeletedAt = now;
            mapping.DeletedBy = deletedBy;
        }

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<byte[]> ExportCsvAsync(int? accountId, string? search)
    {
        var rows = await BuildBaseQuery(accountId, search)
            .OrderByDescending(x => x.Access.UpdatedAt ?? x.Access.CreatedAt)
            .ToListAsync();

        var formsByAccessId = await GetFormsByAccessIdAsync(rows.Select(x => x.Access.Id).ToList());
        var sb = new StringBuilder();
        sb.AppendLine("Account Name,User Name,User Email,Vehicle Number,Forms / Pages,Access Start Date,Access End Date,Permissions,Status");

        foreach (var row in rows)
        {
            var forms = formsByAccessId.TryGetValue(row.Access.Id, out var accessForms)
                ? string.Join(" | ", accessForms.Select(x => x.FormName))
                : string.Empty;

            var permissions = string.Join(" | ", new[]
            {
                row.Access.CanViewTracking ? "Tracking" : null,
                row.Access.CanViewReports ? "Reports" : null
            }.Where(x => x != null));

            sb.AppendLine(string.Join(",", new[]
            {
                Csv(row.AccountName),
                Csv(row.UserName),
                Csv(row.UserEmail),
                Csv(row.VehicleNumber),
                Csv(forms),
                Csv(row.Access.AccessStartDate.ToString("yyyy-MM-dd HH:mm:ss")),
                Csv(row.Access.AccessEndDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? string.Empty),
                Csv(permissions),
                Csv(row.Access.IsActive ? "Active" : "Inactive")
            }));
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private IQueryable<VehicleAccessQueryRow> BuildBaseQuery(int? accountId, string? search)
    {
        var query =
            from va in _db.VehicleAccesses.AsNoTracking().ApplyAccountHierarchyFilter(_currentUser)
            join account in _db.Accounts.AsNoTracking().ApplyAccountHierarchyFilter(_currentUser)
                on va.AccountId equals account.AccountId
            join user in _db.Users.AsNoTracking().ApplyAccountHierarchyFilter(_currentUser)
                on va.UserId equals user.UserId.ToString()
            join vehicle in _db.Vehicles.AsNoTracking().ApplyAccountHierarchyFilter(_currentUser)
                on va.VehicleId equals vehicle.Id
            where !va.IsDeleted && !account.IsDeleted && !user.IsDeleted && !vehicle.IsDeleted
            select new VehicleAccessQueryRow
            {
                Access = va,
                AccountName = account.AccountName,
                UserName = (user.FirstName + " " + user.LastName).Trim(),
                UserEmail = user.Email,
                VehicleNumber = vehicle.VehicleNumber
            };

        if (accountId.HasValue)
            query = query.Where(x => x.Access.AccountId == accountId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(x =>
                x.UserName.ToLower().Contains(s) ||
                x.UserEmail.ToLower().Contains(s) ||
                x.VehicleNumber.ToLower().Contains(s));
        }

        return query;
    }

    private async Task<Dictionary<long, List<VehicleAccessFormResponse>>> GetFormsByAccessIdAsync(List<long> accessIds)
    {
        if (accessIds.Count == 0)
            return new Dictionary<long, List<VehicleAccessFormResponse>>();

        var rows = await (
            from mapping in _db.VehicleAccessForms.AsNoTracking()
            join form in _db.Forms.AsNoTracking()
                on mapping.FormId equals form.FormId
            where accessIds.Contains(mapping.VehicleAccessId) &&
                  !mapping.IsDeleted &&
                  !form.IsDeleted
            orderby form.FormName
            select new
            {
                mapping.VehicleAccessId,
                Form = new VehicleAccessFormResponse
                {
                    FormId = form.FormId,
                    FormName = form.FormName
                }
            }).ToListAsync();

        return rows
            .GroupBy(x => x.VehicleAccessId)
            .ToDictionary(x => x.Key, x => x.Select(y => y.Form).ToList());
    }

    private static VehicleAccessResponse MapResponse(
        VehicleAccessQueryRow row,
        Dictionary<long, List<VehicleAccessFormResponse>> formsByAccessId)
    {
        var forms = formsByAccessId.TryGetValue(row.Access.Id, out var accessForms)
            ? accessForms
            : new List<VehicleAccessFormResponse>();

        return new VehicleAccessResponse
        {
            Id = row.Access.Id,
            AccountId = row.Access.AccountId,
            AccountName = row.AccountName,
            UserId = row.Access.UserId,
            UserName = row.UserName,
            UserEmail = row.UserEmail,
            VehicleId = row.Access.VehicleId,
            VehicleNumber = row.VehicleNumber,
            FormIds = forms.Select(x => x.FormId).ToList(),
            Forms = forms,
            AccessStartDate = row.Access.AccessStartDate,
            AccessEndDate = row.Access.AccessEndDate,
            CanViewTracking = row.Access.CanViewTracking,
            CanViewReports = row.Access.CanViewReports,
            IsActive = row.Access.IsActive
        };
    }

    private async Task ValidateCreateAsync(CreateVehicleAccessRequest request)
    {
        if (request.AccountId <= 0)
            throw new ArgumentException("accountId is required.");
        await ValidateCommonAsync(
            request.AccountId,
            request.UserId,
            request.VehicleId,
            request.FormIds,
            request.AccessStartDate,
            request.AccessEndDate,
            request.CreatedBy,
            "createdBy");

        var duplicateExists = await _db.VehicleAccesses
            .ApplyAccountHierarchyFilter(_currentUser)
            .AnyAsync(x =>
                !x.IsDeleted &&
                x.AccountId == request.AccountId &&
                x.UserId == NormalizeGuidString(request.UserId) &&
                x.VehicleId == request.VehicleId);

        if (duplicateExists)
            throw new InvalidOperationException("Duplicate active vehicle access already exists.");
    }

    private async Task ValidateUpdateAsync(long id, int accountId, UpdateVehicleAccessRequest request)
    {
        await ValidateCommonAsync(
            accountId,
            request.UserId,
            request.VehicleId,
            request.FormIds,
            request.AccessStartDate,
            request.AccessEndDate,
            request.UpdatedBy,
            "updatedBy");

        var duplicateExists = await _db.VehicleAccesses
            .ApplyAccountHierarchyFilter(_currentUser)
            .AnyAsync(x =>
                x.Id != id &&
                !x.IsDeleted &&
                x.AccountId == accountId &&
                x.UserId == NormalizeGuidString(request.UserId) &&
                x.VehicleId == request.VehicleId);

        if (duplicateExists)
            throw new InvalidOperationException("Duplicate active vehicle access already exists.");
    }

    private async Task ValidateCommonAsync(
        int accountId,
        string userId,
        int vehicleId,
        List<int> formIds,
        DateTime accessStartDate,
        DateTime? accessEndDate,
        string auditUserId,
        string auditFieldName)
    {
        if (string.IsNullOrWhiteSpace(userId) || !Guid.TryParse(userId, out var userGuid))
            throw new ArgumentException("userId is required and must be a valid GUID.");

        if (vehicleId <= 0)
            throw new ArgumentException("vehicleId is required.");

        var normalizedFormIds = formIds == null
            ? new List<int>()
            : NormalizeFormIds(formIds);

        if (normalizedFormIds.Count == 0)
            throw new ArgumentException("formIds is required and should not be empty.");

        if (accessStartDate == default)
            throw new ArgumentException("accessStartDate is required.");

        if (accessEndDate.HasValue && accessEndDate.Value <= accessStartDate)
            throw new ArgumentException("accessEndDate must be greater than accessStartDate.");

        if (string.IsNullOrWhiteSpace(auditUserId) || !Guid.TryParse(auditUserId, out _))
            throw new ArgumentException($"{auditFieldName} is required and must be a valid GUID.");

        var userExists = await _db.Users
            .AsNoTracking()
            .ApplyAccountHierarchyFilter(_currentUser)
            .AnyAsync(x =>
                x.AccountId == accountId &&
                x.UserId == userGuid &&
                x.Status &&
                !x.IsDeleted);

        if (!userExists)
            throw new ArgumentException("User does not belong to selected account or is inactive.");

        var vehicleExists = await _db.Vehicles
            .AsNoTracking()
            .ApplyAccountHierarchyFilter(_currentUser)
            .AnyAsync(x =>
                x.AccountId == accountId &&
                x.Id == vehicleId &&
                !x.IsDeleted);

        if (!vehicleExists)
            throw new ArgumentException("Vehicle does not belong to selected account.");

        var existingFormCount = await _db.Forms
            .AsNoTracking()
            .Where(x => normalizedFormIds.Contains(x.FormId) && !x.IsDeleted)
            .CountAsync();

        if (existingFormCount != normalizedFormIds.Count)
            throw new ArgumentException("One or more formIds are invalid.");
    }

    private static List<int> NormalizeFormIds(IEnumerable<int> formIds) =>
        formIds
            .Where(x => x > 0)
            .Distinct()
            .ToList();

    private static string NormalizeGuidString(string value) =>
        Guid.Parse(value.Trim()).ToString();

    private static string Csv(string value)
    {
        value ??= string.Empty;
        return $"\"{value.Replace("\"", "\"\"")}\"";
    }

    private class VehicleAccessQueryRow
    {
        public VehicleAccess Access { get; set; } = new();
        public string AccountName { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string VehicleNumber { get; set; } = string.Empty;
    }
}
