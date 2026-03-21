using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

public class BillingPlanService : IBillingPlanService
{
    private readonly IBillingRepository _repo;
    private readonly ICurrentUserService _currentUser;

    public BillingPlanService(IBillingRepository repo, ICurrentUserService currentUser)
    {
        _repo = repo;
        _currentUser = currentUser;
    }

    public async Task<List<PlanResponseDto>> GetPlansAsync(int skip, int take, CancellationToken ct = default)
    {
        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, 200);

        var items = await _repo.Query<BillingPlan>()
            .AsNoTracking()
            .ApplyAccountHierarchyFilter(_currentUser)
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.UpdatedDate ?? x.CreatedDate)
            .Skip(skip)
            .Take(take)
            .Select(x => new PlanResponseDto
            {
                Id = x.Id,
                AccountId = x.AccountId,
                PlanName = x.PlanName,
                Description = x.Description,
                PlanCategoryId = x.PlanCategoryId,
                CurrencyId = x.CurrencyId,
                BillingCycleId = x.BillingCycleId,
                ContractDuration = x.ContractDuration,
                PricingModel = x.PricingModel,
                PlanStatus = x.PlanStatus,
                TierId = x.TierId,
                BaseRate = x.BaseRate,
                MinUnits = x.MinUnits,
                MaxUnits = x.MaxUnits,
                LicensePricePerUnit = x.LicensePricePerUnit,
                DiscountPercentage = x.DiscountPercentage,
                RecurringPlatformFee = x.RecurringPlatformFee,
                RecurringAmcFee = x.RecurringAmcFee,
                CreatedBy = x.CreatedBy,
                UpdatedBy = x.UpdatedBy,
                CreatedDate = x.CreatedDate,
                UpdatedDate = x.UpdatedDate
            })
            .ToListAsync(ct);

        if (!items.Any())
        {
            return items;
        }

        var planIds = items.Select(x => x.Id).ToList();

        var mappings = await _repo.Query<PlanSolution>()
            .AsNoTracking()
            .ApplyAccountHierarchyFilter(_currentUser)
            .Where(x => planIds.Contains(x.PlanId))
            .Select(x => new { x.PlanId, x.SolutionId })
            .ToListAsync(ct);

        var solutionIds = mappings.Select(x => x.SolutionId).Distinct().ToList();

        var solutionNames = await _repo.Query<SolutionMaster>()
            .AsNoTracking()
            .Where(x => solutionIds.Contains(x.Id))
            .Select(x => new { x.Id, x.Name })
            .ToDictionaryAsync(x => x.Id, x => x.Name, ct);

        foreach (var item in items)
        {
            var ids = mappings
                .Where(x => x.PlanId == item.Id)
                .Select(x => x.SolutionId)
                .Distinct()
                .ToList();

            item.SolutionIds = ids;
            item.SolutionNames = ids.Where(solutionNames.ContainsKey).Select(id => solutionNames[id]).ToList();
        }

        return items;
    }

    public async Task<PlanResponseDto?> GetPlanByIdAsync(int id, CancellationToken ct = default)
    {
        var item = await _repo.Query<BillingPlan>()
            .AsNoTracking()
            .ApplyAccountHierarchyFilter(_currentUser)
            .Where(x => !x.IsDeleted && x.Id == id)
            .Select(x => new PlanResponseDto
            {
                Id = x.Id,
                AccountId = x.AccountId,
                PlanName = x.PlanName,
                Description = x.Description,
                PlanCategoryId = x.PlanCategoryId,
                CurrencyId = x.CurrencyId,
                BillingCycleId = x.BillingCycleId,
                ContractDuration = x.ContractDuration,
                PricingModel = x.PricingModel,
                PlanStatus = x.PlanStatus,
                TierId = x.TierId,
                BaseRate = x.BaseRate,
                MinUnits = x.MinUnits,
                MaxUnits = x.MaxUnits,
                LicensePricePerUnit = x.LicensePricePerUnit,
                DiscountPercentage = x.DiscountPercentage,
                RecurringPlatformFee = x.RecurringPlatformFee,
                RecurringAmcFee = x.RecurringAmcFee,
                CreatedBy = x.CreatedBy,
                UpdatedBy = x.UpdatedBy,
                CreatedDate = x.CreatedDate,
                UpdatedDate = x.UpdatedDate
            })
            .FirstOrDefaultAsync(ct);

        if (item == null)
        {
            return null;
        }

        var solutionIds = await _repo.Query<PlanSolution>()
            .AsNoTracking()
            .ApplyAccountHierarchyFilter(_currentUser)
            .Where(x => x.PlanId == id)
            .Select(x => x.SolutionId)
            .Distinct()
            .ToListAsync(ct);

        item.SolutionIds = solutionIds;

        item.SolutionNames = await _repo.Query<SolutionMaster>()
            .AsNoTracking()
            .Where(x => solutionIds.Contains(x.Id))
            .Select(x => x.Name)
            .ToListAsync(ct);

        return item;
    }

    public async Task<int> CreatePlanAsync(CreatePlanDto dto, CancellationToken ct = default)
    {
        var actor = _currentUser.AccountId > 0 ? _currentUser.AccountId : (int?)null;
        var accountId = ResolveAccount(dto.AccountId);
        await ValidatePlanAsync(accountId, dto, null, ct);
        var solutionIds = await ValidateAndNormalizeSolutionIdsAsync(dto.SolutionIds, ct);

        var entity = new BillingPlan
        {
            AccountId = accountId,
            PlanName = dto.PlanName.Trim(),
            Description = dto.Description,
            PlanCategoryId = dto.PlanCategoryId,
            CurrencyId = dto.CurrencyId,
            BillingCycleId = dto.BillingCycleId,
            ContractDuration = dto.ContractDuration,
            PricingModel = dto.PricingModel,
            PlanStatus = dto.PlanStatus,
            TierId = dto.TierId,
            BaseRate = dto.BaseRate,
            MinUnits = dto.MinUnits,
            MaxUnits = dto.MaxUnits,
            LicensePricePerUnit = dto.LicensePricePerUnit,
            DiscountPercentage = dto.DiscountPercentage,
            RecurringPlatformFee = dto.RecurringPlatformFee,
            RecurringAmcFee = dto.RecurringAmcFee,
            IsActive = true,
            CreatedBy = actor,
            UpdatedBy = actor,
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow,
            IsDeleted = false
        };

        await _repo.AddAsync(entity, ct);
        await _repo.SaveChangesAsync(ct);

        var now = DateTime.UtcNow;
        foreach (var solutionId in solutionIds)
        {
            await _repo.AddAsync(new PlanSolution
            {
                PlanId = entity.Id,
                SolutionId = solutionId,
                AccountId = accountId,
                CreatedDate = now,
                UpdatedDate = now
            }, ct);
        }

        await _repo.SaveChangesAsync(ct);

        return entity.Id;
    }

    public async Task<bool> UpdatePlanAsync(int id, UpdatePlanDto dto, CancellationToken ct = default)
    {
        var entity = await _repo.Query<BillingPlan>()
            .ApplyAccountHierarchyFilter(_currentUser)
            .FirstOrDefaultAsync(x => !x.IsDeleted && x.Id == id, ct);

        if (entity == null)
        {
            return false;
        }

        await ValidatePlanAsync(entity.AccountId, dto, id, ct);
        var solutionIds = await ValidateAndNormalizeSolutionIdsAsync(dto.SolutionIds, ct);

        entity.PlanName = dto.PlanName.Trim();
        entity.Description = dto.Description;
        entity.PlanCategoryId = dto.PlanCategoryId;
        entity.CurrencyId = dto.CurrencyId;
        entity.BillingCycleId = dto.BillingCycleId;
        entity.ContractDuration = dto.ContractDuration;
        entity.PricingModel = dto.PricingModel;
        entity.PlanStatus = dto.PlanStatus;
        entity.IsActive = !string.Equals(dto.PlanStatus, "Inactive", StringComparison.OrdinalIgnoreCase);
        entity.TierId = dto.TierId;
        entity.BaseRate = dto.BaseRate;
        entity.MinUnits = dto.MinUnits;
        entity.MaxUnits = dto.MaxUnits;
        entity.LicensePricePerUnit = dto.LicensePricePerUnit;
        entity.DiscountPercentage = dto.DiscountPercentage;
        entity.RecurringPlatformFee = dto.RecurringPlatformFee;
        entity.RecurringAmcFee = dto.RecurringAmcFee;
        entity.UpdatedBy = _currentUser.AccountId > 0 ? _currentUser.AccountId : null;
        entity.UpdatedDate = DateTime.UtcNow;

        var existingSolutions = await _repo.Query<PlanSolution>()
            .Where(x => x.PlanId == id && x.AccountId == entity.AccountId)
            .ToListAsync(ct);

        foreach (var row in existingSolutions)
        {
            _repo.Remove(row);
        }

        var now = DateTime.UtcNow;
        foreach (var solutionId in solutionIds)
        {
            await _repo.AddAsync(new PlanSolution
            {
                PlanId = id,
                SolutionId = solutionId,
                AccountId = entity.AccountId,
                CreatedDate = now,
                UpdatedDate = now
            }, ct);
        }

        await _repo.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeletePlanAsync(int id, CancellationToken ct = default)
    {
        var entity = await _repo.Query<BillingPlan>()
            .ApplyAccountHierarchyFilter(_currentUser)
            .FirstOrDefaultAsync(x => !x.IsDeleted && x.Id == id, ct);

        if (entity == null)
        {
            return false;
        }

        entity.IsDeleted = true;
        entity.IsActive = false;
        entity.PlanStatus = "Inactive";
        entity.UpdatedBy = _currentUser.AccountId > 0 ? _currentUser.AccountId : null;
        entity.UpdatedDate = DateTime.UtcNow;
        entity.DeletedBy = _currentUser.AccountId > 0 ? _currentUser.AccountId : null;
        entity.DeletedAt = DateTime.UtcNow;
        await _repo.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> UpsertPlanFeaturesAsync(int planId, PlanFeatureUpsertDto dto, CancellationToken ct = default)
    {
        var plan = await _repo.Query<BillingPlan>()
            .ApplyAccountHierarchyFilter(_currentUser)
            .FirstOrDefaultAsync(x => !x.IsDeleted && x.Id == planId, ct);

        if (plan == null)
        {
            return false;
        }

        var existing = await _repo.Query<PlanFeature>()
            .Where(x => x.PlanId == planId && x.AccountId == plan.AccountId)
            .ToListAsync(ct);

        foreach (var row in existing)
        {
            _repo.Remove(row);
        }

        var actor = _currentUser.AccountId > 0 ? _currentUser.AccountId : (int?)null;
        var now = DateTime.UtcNow;
        foreach (var feature in dto.Features.Where(x => !string.IsNullOrWhiteSpace(x.FeatureName)))
        {
            await _repo.AddAsync(new PlanFeature
            {
                AccountId = plan.AccountId,
                PlanId = planId,
                FeatureName = feature.FeatureName.Trim(),
                IsEnabled = feature.IsEnabled,
                CreatedBy = actor,
                UpdatedBy = actor,
                CreatedDate = now,
                UpdatedDate = now
            }, ct);
        }

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

    private async Task<List<int>> ValidateAndNormalizeSolutionIdsAsync(List<int>? rawSolutionIds, CancellationToken ct)
    {
        rawSolutionIds ??= new List<int>();

        var solutionIds = rawSolutionIds.Where(x => x > 0).ToList();

        if (solutionIds.Count != solutionIds.Distinct().Count())
        {
            throw new InvalidOperationException("Duplicate SolutionIds are not allowed.");
        }

        if (!solutionIds.Any())
        {
            return solutionIds;
        }

        var existingSolutionIds = await _repo.Query<SolutionMaster>()
            .AsNoTracking()
            .Where(x => solutionIds.Contains(x.Id) && x.IsActive)
            .Select(x => x.Id)
            .ToListAsync(ct);

        if (existingSolutionIds.Count != solutionIds.Count)
        {
            throw new InvalidOperationException("One or more SolutionIds are invalid.");
        }

        return solutionIds;
    }

    private async Task ValidatePlanAsync(int accountId, CreatePlanDto dto, int? existingId, CancellationToken ct)
    {
        if (dto == null)
        {
            throw new InvalidOperationException("Plan payload is required.");
        }

        if (string.IsNullOrWhiteSpace(dto.PlanName))
        {
            throw new InvalidOperationException("Plan name is required.");
        }

        if (dto.MinUnits < 0 || dto.MaxUnits < 0 || dto.MaxUnits < dto.MinUnits)
        {
            throw new InvalidOperationException("Plan units are invalid.");
        }

        if (dto.ContractDuration <= 0)
        {
            throw new InvalidOperationException("Contract duration must be greater than zero.");
        }

        if (dto.CurrencyId <= 0)
        {
            throw new InvalidOperationException("Currency is required.");
        }

        if (dto.BillingCycleId <= 0)
        {
            throw new InvalidOperationException("Billing cycle is required.");
        }

        var accountExists = await _repo.Query<mst_account>()
            .AsNoTracking()
            .ApplyAccountHierarchyFilter(_currentUser)
            .AnyAsync(x => x.AccountId == accountId && !x.IsDeleted, ct);

        if (!accountExists)
        {
            throw new InvalidOperationException("Account not found in accessible hierarchy.");
        }

        var currencyExists = await _repo.Query<Currency>()
            .AsNoTracking()
            .AnyAsync(x => x.CurrencyId == dto.CurrencyId && x.IsActive, ct);

        if (!currencyExists)
        {
            throw new InvalidOperationException("Currency is invalid.");
        }

        var normalizedName = dto.PlanName.Trim().ToLower();
        var duplicateExists = await _repo.Query<BillingPlan>()
            .AsNoTracking()
            .ApplyAccountHierarchyFilter(_currentUser)
            .AnyAsync(x =>
                !x.IsDeleted &&
                x.AccountId == accountId &&
                x.Id != (existingId ?? 0) &&
                x.PlanName.ToLower() == normalizedName, ct);

        if (duplicateExists)
        {
            throw new InvalidOperationException("Plan name already exists for this account.");
        }
    }
}
