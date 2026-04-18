using Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class DeviceTransferService : IDeviceTransferService
{
    private readonly IdentityDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public DeviceTransferService(IdentityDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<DeviceTransferCreateResultDto> CreateAsync(CreateDeviceTransferRequest request)
    {
        if (request == null)
            throw new BadHttpRequestException("Request payload is required.");

        if (request.FromAccountId <= 0)
            throw new BadHttpRequestException("FromAccountId is required.");

        if (request.ToAccountId <= 0)
            throw new BadHttpRequestException("ToAccountId is required.");

        if (request.FromAccountId == request.ToAccountId)
            throw new BadHttpRequestException("FromAccountId and ToAccountId cannot be same.");

        if (request.DeviceIds == null || request.DeviceIds.Count == 0)
            throw new BadHttpRequestException("At least one DeviceId is required.");

        if (!string.IsNullOrWhiteSpace(request.Remarks) && request.Remarks.Trim().Length > 255)
            throw new BadHttpRequestException("Remarks cannot exceed 255 characters.");

        var deviceIds = request.DeviceIds
            .Where(x => x > 0)
            .Distinct()
            .ToList();

        if (deviceIds.Count == 0)
            throw new BadHttpRequestException("DeviceIds must contain valid positive IDs.");

        var fromToAccounts = await ValidateAccountsAsync(request.FromAccountId, request.ToAccountId);
        var isDirectChildTransfer = IsDirectChildTransfer(fromToAccounts.fromAccount, fromToAccounts.toAccount);

        var deviceQuery = _db.Devices
            .AsQueryable();

        if (ShouldApplyHierarchyScope())
            deviceQuery = deviceQuery.ApplyAccountHierarchyFilter(_currentUser);

        var devices = await deviceQuery
            .Where(x => deviceIds.Contains(x.Id) && !x.IsDeleted)
            .Select(x => new
            {
                x.Id,
                x.AccountId
            })
            .ToListAsync();

        if (devices.Count != deviceIds.Count)
        {
            var foundDeviceIds = devices.Select(x => x.Id).ToHashSet();
            var missing = deviceIds.Where(x => !foundDeviceIds.Contains(x)).ToList();
            throw new BadHttpRequestException($"Invalid or inaccessible DeviceId(s): {string.Join(", ", missing)}");
        }

        var invalidOwnerDevices = devices
            .Where(x => x.AccountId != request.FromAccountId)
            .Select(x => x.Id)
            .ToList();

        if (invalidOwnerDevices.Count > 0)
            throw new InvalidOperationException(
                $"Device(s) do not belong to source account: {string.Join(", ", invalidOwnerDevices)}");

        var pendingDeviceIds = await (
            from item in _db.DeviceTransferItems
            join transferHeader in _db.DeviceTransfers on item.TransferId equals transferHeader.Id
            where deviceIds.Contains(item.DeviceId)
                  && transferHeader.Status == DeviceTransferStatuses.Pending
            select item.DeviceId
        ).Distinct().ToListAsync();

        if (pendingDeviceIds.Count > 0)
        {
            throw new InvalidOperationException(
                $"Device(s) already in pending transfer: {string.Join(", ", pendingDeviceIds)}");
        }

        var transferCode = await GenerateTransferCodeAsync();
        var now = DateTime.UtcNow;

        await using var transaction = await _db.Database.BeginTransactionAsync();

        var transfer = new device_transfer
        {
            TransferCode = transferCode,
            FromAccountId = request.FromAccountId,
            ToAccountId = request.ToAccountId,
            Status = isDirectChildTransfer
                ? DeviceTransferStatuses.Completed
                : DeviceTransferStatuses.Pending,
            Remarks = request.Remarks?.Trim(),
            CreatedBy = request.CreatedBy,
            CreatedAt = now
        };

        _db.DeviceTransfers.Add(transfer);
        await _db.SaveChangesAsync();

        var items = deviceIds.Select(deviceId => new device_transfer_item
        {
            TransferId = transfer.Id,
            DeviceId = deviceId,
            CreatedAt = now
        }).ToList();

        _db.DeviceTransferItems.AddRange(items);
        await _db.SaveChangesAsync();

        if (isDirectChildTransfer)
        {
            var deviceEntities = await _db.Devices
                .Where(x => deviceIds.Contains(x.Id) && !x.IsDeleted)
                .ToListAsync();

            foreach (var device in deviceEntities)
            {
                device.AccountId = request.ToAccountId;
                device.updatedAt = now;
                device.updatedBy = request.CreatedBy > 0 ? request.CreatedBy : null;
            }

            transfer.UpdatedAt = now;
            transfer.UpdatedBy = request.CreatedBy > 0 ? request.CreatedBy : null;
            await _db.SaveChangesAsync();
        }

        await transaction.CommitAsync();

        return new DeviceTransferCreateResultDto
        {
            TransferId = transfer.Id,
            TransferCode = transfer.TransferCode,
            Status = transfer.Status
        };
    }

    public async Task<DeviceTransferActionResultDto> ApproveAsync(int transferId, UpdateDeviceTransferStatusRequest? request)
    {
        if (transferId <= 0)
            throw new BadHttpRequestException("Transfer ID is invalid.");

        await using var transaction = await _db.Database.BeginTransactionAsync();

        var transfer = await GetTransferForManageAsync(transferId);
        ValidatePendingState(transfer, "approve");

        var deviceIds = await _db.DeviceTransferItems
            .Where(x => x.TransferId == transferId)
            .Select(x => x.DeviceId)
            .Distinct()
            .ToListAsync();

        if (deviceIds.Count == 0)
            throw new InvalidOperationException("Transfer has no device items.");

        var deviceQuery = _db.Devices.AsQueryable();
        if (ShouldApplyHierarchyScope())
            deviceQuery = deviceQuery.ApplyAccountHierarchyFilter(_currentUser);

        var devices = await deviceQuery
            .Where(x => deviceIds.Contains(x.Id) && !x.IsDeleted)
            .ToListAsync();

        if (devices.Count != deviceIds.Count)
            throw new InvalidOperationException("One or more devices are not available for transfer.");

        var ownershipMismatch = devices
            .Where(x => x.AccountId != transfer.FromAccountId)
            .Select(x => x.Id)
            .ToList();

        if (ownershipMismatch.Count > 0)
        {
            throw new InvalidOperationException(
                $"Device ownership changed before approval. DeviceId(s): {string.Join(", ", ownershipMismatch)}");
        }

        var now = DateTime.UtcNow;
        var updatedBy = NormalizeUpdatedBy(request?.UpdatedBy);

        foreach (var device in devices)
        {
            device.AccountId = transfer.ToAccountId;
            device.updatedAt = now;
            device.updatedBy = updatedBy;
        }

        transfer.Status = DeviceTransferStatuses.Completed;
        transfer.UpdatedAt = now;
        transfer.UpdatedBy = updatedBy;

        if (!string.IsNullOrWhiteSpace(request?.Remarks))
            transfer.Remarks = request!.Remarks!.Trim();

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return new DeviceTransferActionResultDto
        {
            TransferId = transfer.Id,
            Status = transfer.Status
        };
    }

    public async Task<DeviceTransferActionResultDto> CancelAsync(int transferId, UpdateDeviceTransferStatusRequest? request)
    {
        if (transferId <= 0)
            throw new BadHttpRequestException("Transfer ID is invalid.");

        var transfer = await GetTransferForManageAsync(transferId);

        if (transfer.Status == DeviceTransferStatuses.Completed)
            throw new InvalidOperationException("Completed transfer cannot be cancelled.");

        if (transfer.Status == DeviceTransferStatuses.Cancelled)
            throw new InvalidOperationException("Transfer is already cancelled.");

        var now = DateTime.UtcNow;
        var updatedBy = NormalizeUpdatedBy(request?.UpdatedBy);

        transfer.Status = DeviceTransferStatuses.Cancelled;
        transfer.UpdatedAt = now;
        transfer.UpdatedBy = updatedBy;

        if (!string.IsNullOrWhiteSpace(request?.Remarks))
            transfer.Remarks = request!.Remarks!.Trim();

        await _db.SaveChangesAsync();

        return new DeviceTransferActionResultDto
        {
            TransferId = transfer.Id,
            Status = transfer.Status
        };
    }

    public async Task<DeviceTransferDto?> GetByIdAsync(int transferId)
    {
        if (transferId <= 0)
            throw new BadHttpRequestException("Transfer ID is invalid.");

        var transfer = await ApplyTransferReadScope(_db.DeviceTransfers.AsNoTracking())
            .FirstOrDefaultAsync(x => x.Id == transferId);

        if (transfer == null)
            return null;

        var items = await _db.DeviceTransferItems
            .AsNoTracking()
            .Where(x => x.TransferId == transfer.Id)
            .ToListAsync();

        var deviceIds = items.Select(x => x.DeviceId).Distinct().ToList();
        var devices = await _db.Devices
            .AsNoTracking()
            .Where(x => deviceIds.Contains(x.Id))
            .Select(x => new
            {
                x.Id,
                x.DeviceNo,
                x.DeviceImeiOrSerial
            })
            .ToDictionaryAsync(x => x.Id, x => new DeviceTransferItemDto
            {
                DeviceId = x.Id,
                DeviceNo = x.DeviceNo,
                DeviceImeiOrSerial = x.DeviceImeiOrSerial
            });

        var accountIds = new[] { transfer.FromAccountId, transfer.ToAccountId };
        var accountNames = await _db.Accounts
            .AsNoTracking()
            .Where(x => accountIds.Contains(x.AccountId))
            .ToDictionaryAsync(x => x.AccountId, x => x.AccountName);

        return new DeviceTransferDto
        {
            Id = transfer.Id,
            TransferCode = transfer.TransferCode,
            FromAccountId = transfer.FromAccountId,
            FromAccountName = accountNames.GetValueOrDefault(transfer.FromAccountId, string.Empty),
            ToAccountId = transfer.ToAccountId,
            ToAccountName = accountNames.GetValueOrDefault(transfer.ToAccountId, string.Empty),
            Status = transfer.Status,
            Remarks = transfer.Remarks,
            CreatedBy = transfer.CreatedBy,
            CreatedAt = transfer.CreatedAt,
            UpdatedBy = transfer.UpdatedBy,
            UpdatedAt = transfer.UpdatedAt,
            DeviceCount = items.Count,
            Items = items
                .Select(x => devices.TryGetValue(x.DeviceId, out var dto)
                    ? dto
                    : new DeviceTransferItemDto { DeviceId = x.DeviceId })
                .ToList()
        };
    }

    public async Task<DeviceTransferListUiResponseDto> GetTransfersAsync(
        int page,
        int pageSize,
        int? accountId = null,
        int? fromAccountId = null,
        int? toAccountId = null,
        string? status = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        string? search = null)
    {
        if (page <= 0) page = 1;
        if (pageSize <= 0) pageSize = 10;

        if (fromDate.HasValue && toDate.HasValue && fromDate.Value.Date > toDate.Value.Date)
            throw new BadHttpRequestException("FromDate cannot be greater than ToDate.");

        var normalizedStatus = NormalizeStatus(status);

        var query = ApplyTransferReadScope(_db.DeviceTransfers.AsNoTracking());

        if (accountId.HasValue)
        {
            query = query.Where(x =>
                x.FromAccountId == accountId.Value ||
                x.ToAccountId == accountId.Value);
        }

        if (fromAccountId.HasValue)
            query = query.Where(x => x.FromAccountId == fromAccountId.Value);

        if (toAccountId.HasValue)
            query = query.Where(x => x.ToAccountId == toAccountId.Value);

        if (!string.IsNullOrWhiteSpace(normalizedStatus))
            query = query.Where(x => x.Status == normalizedStatus);

        if (fromDate.HasValue)
        {
            var startDate = fromDate.Value.Date;
            query = query.Where(x => x.CreatedAt >= startDate);
        }

        if (toDate.HasValue)
        {
            var endExclusive = toDate.Value.Date.AddDays(1);
            query = query.Where(x => x.CreatedAt < endExclusive);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(x =>
                x.TransferCode.ToLower().Contains(term) ||
                (x.Remarks != null && x.Remarks.ToLower().Contains(term)));
        }

        var summaryData = await query
            .GroupBy(_ => 1)
            .Select(g => new DeviceTransferSummaryDto
            {
                TotalTransfers = g.Count(),
                Pending = g.Count(x => x.Status == DeviceTransferStatuses.Pending),
                Completed = g.Count(x => x.Status == DeviceTransferStatuses.Completed),
                Cancelled = g.Count(x => x.Status == DeviceTransferStatuses.Cancelled)
            })
            .FirstOrDefaultAsync() ?? new DeviceTransferSummaryDto();

        var total = summaryData.TotalTransfers;

        var headers = await query
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.TransferCode,
                x.FromAccountId,
                x.ToAccountId,
                x.Status,
                x.Remarks,
                x.CreatedAt,
                x.UpdatedAt
            })
            .ToListAsync();

        var transferIds = headers.Select(x => x.Id).ToList();

        var deviceCounts = await _db.DeviceTransferItems
            .AsNoTracking()
            .Where(x => transferIds.Contains(x.TransferId))
            .GroupBy(x => x.TransferId)
            .Select(g => new
            {
                TransferId = g.Key,
                Count = g.Count()
            })
            .ToDictionaryAsync(x => x.TransferId, x => x.Count);

        var accountIds = headers
            .SelectMany(x => new[] { x.FromAccountId, x.ToAccountId })
            .Distinct()
            .ToList();

        var accountNames = await _db.Accounts
            .AsNoTracking()
            .Where(x => accountIds.Contains(x.AccountId))
            .ToDictionaryAsync(x => x.AccountId, x => x.AccountName);

        var listItems = headers.Select(x => new DeviceTransferListItemDto
        {
            Id = x.Id,
            TransferCode = x.TransferCode,
            FromAccountId = x.FromAccountId,
            FromAccountName = accountNames.GetValueOrDefault(x.FromAccountId, string.Empty),
            ToAccountId = x.ToAccountId,
            ToAccountName = accountNames.GetValueOrDefault(x.ToAccountId, string.Empty),
            Status = x.Status,
            Remarks = x.Remarks,
            DeviceCount = deviceCounts.GetValueOrDefault(x.Id, 0),
            CreatedAt = x.CreatedAt,
            UpdatedAt = x.UpdatedAt
        }).ToList();

        return new DeviceTransferListUiResponseDto
        {
            Summary = summaryData,
            Transfers = new PagedResultDto<DeviceTransferListItemDto>
            {
                Items = listItems,
                TotalRecords = total,
                Page = page,
                PageSize = pageSize
            }
        };
    }

    private async Task<(mst_account fromAccount, mst_account toAccount)> ValidateAccountsAsync(int fromAccountId, int toAccountId)
    {
        var fromAccount = await _db.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.AccountId == fromAccountId);

        if (fromAccount == null)
            throw new BadHttpRequestException("Invalid FromAccountId.");

        var toAccount = await _db.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.AccountId == toAccountId);

        if (toAccount == null)
            throw new BadHttpRequestException("Invalid ToAccountId.");

        if (ShouldApplyHierarchyScope())
        {
            var accessible = _currentUser.AccessibleAccountIds;
            if (accessible == null || !accessible.Contains(fromAccountId))
                throw new BadHttpRequestException("Invalid or inaccessible FromAccountId.");
        }

        return (fromAccount, toAccount);
    }

    private async Task<device_transfer> GetTransferForManageAsync(int transferId)
    {
        var transfer = await ApplyTransferManageScope(_db.DeviceTransfers)
            .FirstOrDefaultAsync(x => x.Id == transferId);

        if (transfer == null)
            throw new KeyNotFoundException("Transfer not found.");

        return transfer;
    }

    private IQueryable<device_transfer> ApplyTransferReadScope(IQueryable<device_transfer> query)
    {
        if (!ShouldApplyHierarchyScope())
            return query;

        var accessible = _currentUser.AccessibleAccountIds;
        if (accessible == null || accessible.Count == 0)
            return query.Where(_ => false);

        return query.Where(x =>
            accessible.Contains(x.FromAccountId) ||
            accessible.Contains(x.ToAccountId));
    }

    private IQueryable<device_transfer> ApplyTransferManageScope(IQueryable<device_transfer> query)
    {
        if (!ShouldApplyHierarchyScope())
            return query;

        var accessible = _currentUser.AccessibleAccountIds;
        if (accessible == null || accessible.Count == 0)
            return query.Where(_ => false);

        return query.Where(x => accessible.Contains(x.FromAccountId));
    }

    private bool ShouldApplyHierarchyScope()
    {
        return _currentUser.IsAuthenticated && !_currentUser.IsSystemRole;
    }

    private static bool IsDirectChildTransfer(mst_account fromAccount, mst_account toAccount)
    {
        return toAccount.ParentAccountId.HasValue &&
               toAccount.ParentAccountId.Value == fromAccount.AccountId;
    }

    private static string NormalizeStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return string.Empty;

        var normalized = status.Trim();
        if (normalized.Equals(DeviceTransferStatuses.Pending, StringComparison.OrdinalIgnoreCase))
            return DeviceTransferStatuses.Pending;

        if (normalized.Equals(DeviceTransferStatuses.Completed, StringComparison.OrdinalIgnoreCase))
            return DeviceTransferStatuses.Completed;

        if (normalized.Equals(DeviceTransferStatuses.Cancelled, StringComparison.OrdinalIgnoreCase))
            return DeviceTransferStatuses.Cancelled;

        throw new BadHttpRequestException("Invalid status filter. Allowed: Pending, Completed, Cancelled.");
    }

    private static int? NormalizeUpdatedBy(int? updatedBy)
    {
        if (!updatedBy.HasValue || updatedBy.Value <= 0)
            return null;

        return updatedBy.Value;
    }

    private static void ValidatePendingState(device_transfer transfer, string action)
    {
        if (transfer.Status == DeviceTransferStatuses.Completed)
            throw new InvalidOperationException($"Transfer is already completed. Cannot {action}.");

        if (transfer.Status == DeviceTransferStatuses.Cancelled)
            throw new InvalidOperationException($"Transfer is cancelled. Cannot {action}.");
    }

    private async Task<string> GenerateTransferCodeAsync()
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var code = $"TRF-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}"[..40].ToUpperInvariant();
            var exists = await _db.DeviceTransfers.AnyAsync(x => x.TransferCode == code);
            if (!exists)
                return code;
        }

        return $"TRF-{Guid.NewGuid():N}"[..40].ToUpperInvariant();
    }
}
