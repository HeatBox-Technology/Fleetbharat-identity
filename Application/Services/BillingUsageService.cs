using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public class BillingUsageService : IBillingUsageService
{
    private readonly IBillingRepository _repo;
    private readonly ICurrentUserService _currentUser;

    public BillingUsageService(IBillingRepository repo, ICurrentUserService currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public async Task<int> RecordUsageAsync(UsageRecordCreateDto dto, CancellationToken ct = default)
    {
        var accountId = ResolveAccount(dto.AccountId);
        var subscription = await _repo.Query<AccountSubscription>()
            .ApplyAccountHierarchyFilter(_currentUser)
            .FirstOrDefaultAsync(x => x.Id == dto.SubscriptionId && x.AccountId == accountId, ct);

        if (subscription == null)
        {
            throw new InvalidOperationException("Subscription not found for this hierarchy.");
        }

        var actor = _currentUser.AccountId > 0 ? _currentUser.AccountId : (int?)null;
        var row = new UsageRecord
        {
            AccountId = accountId,
            SubscriptionId = dto.SubscriptionId,
            UsageType = dto.UsageType.Trim(),
            UnitsConsumed = dto.UnitsConsumed,
            UsageDate = dto.UsageDate,
            CreatedBy = actor,
            UpdatedBy = actor,
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };

        await _repo.AddAsync(row, ct);
        await _repo.SaveChangesAsync(ct);
        return row.Id;
    }

    public async Task<List<UsageRecordResponseDto>> GetUsageByAccountAsync(int accountId, int skip, int take, CancellationToken ct = default)
    {
        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, 500);

        return await _repo.Query<UsageRecord>()
            .AsNoTracking()
            .ApplyAccountHierarchyFilter(_currentUser)
            .Where(x => x.AccountId == accountId)
            .OrderByDescending(x => x.UsageDate)
            .Skip(skip)
            .Take(take)
            .Select(x => new UsageRecordResponseDto
            {
                Id = x.Id,
                AccountId = x.AccountId,
                SubscriptionId = x.SubscriptionId,
                UsageType = x.UsageType,
                UnitsConsumed = x.UnitsConsumed,
                UsageDate = x.UsageDate,
                CreatedBy = x.CreatedBy,
                UpdatedBy = x.UpdatedBy,
                CreatedDate = x.CreatedDate,
                UpdatedDate = x.UpdatedDate
            })
            .ToListAsync(ct);
    }

    private int ResolveAccount(int requestAccountId)
    {
        if (_currentUser.IsSystemRole && requestAccountId > 0)
        {
            return requestAccountId;
        }

        return _currentUser.AccountId;
    }
}
