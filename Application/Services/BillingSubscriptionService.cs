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

        await ValidateSubscriptionAsync(accountId, dto, plan, ct);
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
            IsActive = string.Equals(dto.Status, "Active", StringComparison.OrdinalIgnoreCase),
            CreatedBy = actor,
            UpdatedBy = actor,
            CreatedDate = now,
            UpdatedDate = now,
            IsDeleted = false
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
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.UpdatedDate ?? x.CreatedDate)
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
            .Where(x => !x.IsDeleted && x.AccountId == accountId)
            .OrderByDescending(x => x.UpdatedDate ?? x.CreatedDate)
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

    public async Task<bool> DeleteSubscriptionAsync(int id, CancellationToken ct = default)
    {
        var entity = await _repo.Query<AccountSubscription>()
            .ApplyAccountHierarchyFilter(_currentUser)
            .FirstOrDefaultAsync(x => !x.IsDeleted && x.Id == id, ct);

        if (entity == null)
        {
            return false;
        }

        entity.IsDeleted = true;
        entity.IsActive = false;
        entity.Status = "Inactive";
        entity.UpdatedBy = _currentUser.AccountId > 0 ? _currentUser.AccountId : null;
        entity.UpdatedDate = DateTime.UtcNow;
        entity.DeletedBy = _currentUser.AccountId > 0 ? _currentUser.AccountId : null;
        entity.DeletedAt = DateTime.UtcNow;

        await _repo.SaveChangesAsync(ct);
        return true;
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

    private async Task ValidateSubscriptionAsync(
        int accountId,
        AccountSubscriptionMapPlanDto dto,
        BillingPlan plan,
        CancellationToken ct)
    {
        if (dto == null)
        {
            throw new InvalidOperationException("Subscription payload is required.");
        }

        if (dto.PlanId <= 0)
        {
            throw new InvalidOperationException("Plan is required.");
        }

        if (dto.Units <= 0)
        {
            throw new InvalidOperationException("Units must be greater than zero.");
        }

        if (dto.EndDate.Date < dto.StartDate.Date)
        {
            throw new InvalidOperationException("End date must be greater than or equal to start date.");
        }

        if (string.IsNullOrWhiteSpace(dto.Status))
        {
            throw new InvalidOperationException("Subscription status is required.");
        }

        var accountExists = await _repo.Query<mst_account>()
            .AsNoTracking()
            .ApplyAccountHierarchyFilter(_currentUser)
            .AnyAsync(x => x.AccountId == accountId && !x.IsDeleted, ct);

        if (!accountExists)
        {
            throw new InvalidOperationException("Account not found in accessible hierarchy.");
        }

        if (plan.AccountId != accountId)
        {
            throw new InvalidOperationException("Selected plan does not belong to the target account.");
        }

        var duplicateExists = await _repo.Query<AccountSubscription>()
            .AsNoTracking()
            .ApplyAccountHierarchyFilter(_currentUser)
            .AnyAsync(x =>
                !x.IsDeleted &&
                x.AccountId == accountId &&
                x.PlanId == dto.PlanId &&
                x.Status == dto.Status &&
                x.StartDate.Date == dto.StartDate.Date &&
                x.EndDate.Date == dto.EndDate.Date, ct);

        if (duplicateExists)
        {
            throw new InvalidOperationException("A matching subscription already exists for this account.");
        }
    }
}
