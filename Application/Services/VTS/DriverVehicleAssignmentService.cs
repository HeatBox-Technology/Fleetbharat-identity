using System;
using System.Linq;
using System.Threading.Tasks;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

public class DriverVehicleAssignmentService : IDriverVehicleAssignmentService
{
    private readonly IdentityDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DriverVehicleAssignmentService(
        IdentityDbContext db,
        ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<int> CreateAsync(CreateDriverVehicleAssignmentDto dto)
    {
        await ValidateRequestAsync(
            dto.AccountContextId,
            dto.DriverId,
            dto.VehicleId,
            dto.AssignmentLogic,
            dto.StartTime,
            dto.ExpectedEnd);

        var now = DateTime.UtcNow;
        var entity = new map_driver_vehicle_assignment
        {
            AccountId = dto.AccountContextId,
            DriverId = dto.DriverId,
            VehicleId = dto.VehicleId,
            AssignmentLogic = NormalizeAssignmentLogic(dto.AssignmentLogic),
            StartTime = dto.StartTime,
            ExpectedEnd = dto.ExpectedEnd,
            DispatcherNotes = NormalizeNotes(dto.DispatcherNotes),
            CreatedBy = dto.CreatedBy > 0 ? dto.CreatedBy : _currentUser.AccountId,
            CreatedAt = now,
            CreatedByUser = dto.CreatedByUser ?? (_currentUser.UserId == Guid.Empty ? null : _currentUser.UserId),
            CreatedAtUser = now,
            IsDeleted = false
        };

        _db.DriverVehicleAssignments.Add(entity);
        await _db.SaveChangesAsync();

        return entity.Id;
    }

    public async Task<bool> UpdateAsync(int id, UpdateDriverVehicleAssignmentDto dto)
    {
        var entity = await _db.DriverVehicleAssignments
            .ApplyAccountHierarchyFilter(_currentUser)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (entity == null)
            return false;

        await ValidateRequestAsync(
            dto.AccountContextId,
            dto.DriverId,
            dto.VehicleId,
            dto.AssignmentLogic,
            dto.StartTime,
            dto.ExpectedEnd);

        entity.AccountId = dto.AccountContextId;
        entity.DriverId = dto.DriverId;
        entity.VehicleId = dto.VehicleId;
        entity.AssignmentLogic = NormalizeAssignmentLogic(dto.AssignmentLogic);
        entity.StartTime = dto.StartTime;
        entity.ExpectedEnd = dto.ExpectedEnd;
        entity.DispatcherNotes = NormalizeNotes(dto.DispatcherNotes);
        entity.UpdatedBy = dto.UpdatedBy ?? _currentUser.AccountId;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedByUser = dto.UpdatedByUser ?? (_currentUser.UserId == Guid.Empty ? null : _currentUser.UserId);
        entity.UpdatedAtUser = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<PagedResultDto<DriverVehicleAssignmentDto>> GetAllAsync(
        int page,
        int pageSize,
        int? accountContextId,
        string? search)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;

        var query = BaseQuery();

        if (accountContextId.HasValue)
            query = query.Where(x => x.AccountId == accountContextId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(x =>
                x.AssignmentLogic.ToLower().Contains(s) ||
                (x.DispatcherNotes != null && x.DispatcherNotes.ToLower().Contains(s)) ||
                x.Driver.Name.ToLower().Contains(s) ||
                x.Vehicle.VehicleNumber.ToLower().Contains(s));
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(MapToDtoExpression())
            .ToListAsync();

        return new PagedResultDto<DriverVehicleAssignmentDto>
        {
            Items = items,
            TotalRecords = total,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)total / pageSize)
        };
    }

    public async Task<DriverVehicleAssignmentDto?> GetByIdAsync(int id)
    {
        return await BaseQuery()
            .Where(x => x.Id == id)
            .Select(MapToDtoExpression())
            .FirstOrDefaultAsync();
    }

    public async Task<bool> DeleteAsync(int id)
    {
        return await SoftDeleteInternalAsync(id);
    }

    public async Task<bool> SoftDeleteAsync(int id)
    {
        return await SoftDeleteInternalAsync(id);
    }

    private IQueryable<map_driver_vehicle_assignment> BaseQuery()
    {
        return _db.DriverVehicleAssignments
            .AsNoTracking()
            .ApplyAccountHierarchyFilter(_currentUser)
            .Where(x => !x.IsDeleted);
    }

    private async Task<bool> SoftDeleteInternalAsync(int id)
    {
        var entity = await _db.DriverVehicleAssignments
            .ApplyAccountHierarchyFilter(_currentUser)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

        if (entity == null)
            return false;

        var now = DateTime.UtcNow;
        entity.IsDeleted = true;
        entity.DeletedBy = _currentUser.AccountId > 0 ? _currentUser.AccountId : null;
        entity.DeletedAt = now;
        entity.DeletedByUser = _currentUser.UserId == Guid.Empty ? null : _currentUser.UserId;
        entity.DeletedAtUser = now;
        entity.UpdatedBy = _currentUser.AccountId > 0 ? _currentUser.AccountId : entity.UpdatedBy;
        entity.UpdatedAt = now;
        entity.UpdatedByUser = _currentUser.UserId == Guid.Empty ? entity.UpdatedByUser : _currentUser.UserId;
        entity.UpdatedAtUser = now;

        await _db.SaveChangesAsync();
        return true;
    }

    private async Task ValidateRequestAsync(
        int accountId,
        int driverId,
        int vehicleId,
        string assignmentLogic,
        DateTime startTime,
        DateTime? expectedEnd)
    {
        if (accountId <= 0)
            throw new InvalidOperationException("AccountContextId is required.");

        if (!_currentUser.IsSystem && !_currentUser.AccessibleAccountIds.Contains(accountId))
            throw new InvalidOperationException("Unauthorized account access.");

        var accountExists = await _db.Accounts
            .ApplyAccountHierarchyFilter(_currentUser)
            .AnyAsync(x => x.AccountId == accountId && !x.IsDeleted);

        if (!accountExists)
            throw new InvalidOperationException("Invalid AccountContextId.");

        var driverExists = await _db.Drivers
            .ApplyAccountHierarchyFilter(_currentUser)
            .AnyAsync(x => x.DriverId == driverId && x.AccountId == accountId && !x.IsDeleted);

        if (!driverExists)
            throw new InvalidOperationException("Invalid DriverId.");

        var vehicleExists = await _db.Vehicles
            .ApplyAccountHierarchyFilter(_currentUser)
            .AnyAsync(x => x.Id == vehicleId && x.AccountId == accountId && !x.IsDeleted);

        if (!vehicleExists)
            throw new InvalidOperationException("Invalid VehicleId.");

        if (string.IsNullOrWhiteSpace(assignmentLogic))
            throw new InvalidOperationException("AssignmentLogic is required.");

        if (expectedEnd.HasValue && expectedEnd.Value < startTime)
            throw new InvalidOperationException("ExpectedEnd cannot be earlier than StartTime.");
    }

    private static string NormalizeAssignmentLogic(string value)
    {
        return value.Trim();
    }

    private static string? NormalizeNotes(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static System.Linq.Expressions.Expression<Func<map_driver_vehicle_assignment, DriverVehicleAssignmentDto>> MapToDtoExpression()
    {
        return x => new DriverVehicleAssignmentDto
        {
            Id = x.Id,
            AccountContextId = x.AccountId,
            DriverId = x.DriverId,
            DriverName = x.Driver != null ? x.Driver.Name : string.Empty,
            VehicleId = x.VehicleId,
            VehicleNumber = x.Vehicle != null ? x.Vehicle.VehicleNumber : string.Empty,
            AssignmentLogic = x.AssignmentLogic,
            StartTime = x.StartTime,
            ExpectedEnd = x.ExpectedEnd,
            DispatcherNotes = x.DispatcherNotes,
            CreatedBy = x.CreatedBy,
            CreatedAt = x.CreatedAt,
            UpdatedBy = x.UpdatedBy,
            UpdatedAt = x.UpdatedAt,
            DeletedBy = x.DeletedBy,
            DeletedAt = x.DeletedAt,
            CreatedByUser = x.CreatedByUser,
            CreatedAtUser = x.CreatedAtUser,
            UpdatedByUser = x.UpdatedByUser,
            UpdatedAtUser = x.UpdatedAtUser,
            DeletedByUser = x.DeletedByUser,
            DeletedAtUser = x.DeletedAtUser,
            IsDeleted = x.IsDeleted
        };
    }
}
