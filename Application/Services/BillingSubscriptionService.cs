using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public class BillingSubscriptionService : IBillingSubscriptionService
{
    private readonly IBillingRepository _repo;
    private readonly ICurrentUserService _currentUser;

    public BillingSubscriptionService(IBillingRepository repo, ICurrentUserService currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public async Task<int> MapPlanAsync(AccountSubscriptionMapPlanDto dto, CancellationToken ct = default)
    {
        var accountId = ResolveAccount(dto.AccountId);
        var accessible = _currentUser.IsSystemRole || _currentUser.AccessibleAccountIds.Contains(accountId);
        if (!accessible)
        {
            throw new UnauthorizedAccessException("Account access denied.");
        }

        var plan = await _repo.Query<BillingPlan>()
            .ApplyAccountHierarchyFilter(_currentUser)
            .FirstOrDefaultAsync(x => !x.IsDeleted && x.Id == dto.PlanId, ct);

        if (plan == null)
        {
            throw new InvalidOperationException("Plan not found in accessible hierarchy.");
        }

        var actor = _currentUser.AccountId > 0 ? _currentUser.AccountId : (int?)null;
        var now = DateTime.UtcNow;

        var subscription = new AccountSubscription
        {
            AccountId = accountId,
            PlanId = dto.PlanId,
            Units = dto.Units,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Status = dto.Status,
            NextBillingDate = CalculateNextBillingDate(dto.StartDate, plan.BillingCycleId),
            CreatedBy = actor,
            UpdatedBy = actor,
            CreatedDate = now,
            UpdatedDate = now
        };

        await _repo.AddAsync(subscription, ct);
        await _repo.SaveChangesAsync(ct);
        return subscription.Id;
    }

    public async Task<List<AccountSubscriptionResponseDto>> GetSubscriptionsAsync(int skip, int take, CancellationToken ct = default)
    {
        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, 200);

        return await _repo.Query<AccountSubscription>()
            .AsNoTracking()
            .ApplyAccountHierarchyFilter(_currentUser)
            .OrderByDescending(x => x.CreatedDate)
            .Skip(skip)
            .Take(take)
            .Select(x => new AccountSubscriptionResponseDto
            {
                Id = x.Id,
                AccountId = x.AccountId,
                PlanId = x.PlanId,
                Units = x.Units,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                Status = x.Status,
                NextBillingDate = x.NextBillingDate,
                CreatedBy = x.CreatedBy,
                UpdatedBy = x.UpdatedBy,
                CreatedDate = x.CreatedDate,
                UpdatedDate = x.UpdatedDate
            })
            .ToListAsync(ct);
    }

    public async Task<List<AccountSubscriptionResponseDto>> GetSubscriptionsByAccountAsync(int accountId, int skip, int take, CancellationToken ct = default)
    {
        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, 200);

        return await _repo.Query<AccountSubscription>()
            .AsNoTracking()
            .ApplyAccountHierarchyFilter(_currentUser)
            .Where(x => x.AccountId == accountId)
            .OrderByDescending(x => x.CreatedDate)
            .Skip(skip)
            .Take(take)
            .Select(x => new AccountSubscriptionResponseDto
            {
                Id = x.Id,
                AccountId = x.AccountId,
                PlanId = x.PlanId,
                Units = x.Units,
                StartDate = x.StartDate,
                EndDate = x.EndDate,
                Status = x.Status,
                NextBillingDate = x.NextBillingDate,
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

    private static DateTime CalculateNextBillingDate(DateTime startDate, int billingCycleId)
    {
        return billingCycleId switch
        {
            1 => startDate.Date.AddMonths(1),
            2 => startDate.Date.AddMonths(3),
            3 => startDate.Date.AddYears(1),
            _ => startDate.Date.AddMonths(1)
        };
    }
}
