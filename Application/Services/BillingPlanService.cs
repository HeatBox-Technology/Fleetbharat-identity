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

        return await _repo.Query<BillingPlan>()
            .AsNoTracking()
            .ApplyAccountHierarchyFilter(_currentUser)
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.CreatedDate)
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
    }

    public async Task<PlanResponseDto?> GetPlanByIdAsync(int id, CancellationToken ct = default)
    {
        return await _repo.Query<BillingPlan>()
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
    }

    public async Task<int> CreatePlanAsync(CreatePlanDto dto, CancellationToken ct = default)
    {
        var actor = _currentUser.AccountId > 0 ? _currentUser.AccountId : (int?)null;
        var accountId = ResolveAccount(dto.AccountId);

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
            CreatedBy = actor,
            UpdatedBy = actor,
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };

        await _repo.AddAsync(entity, ct);
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

        entity.PlanName = dto.PlanName.Trim();
        entity.Description = dto.Description;
        entity.PlanCategoryId = dto.PlanCategoryId;
        entity.CurrencyId = dto.CurrencyId;
        entity.BillingCycleId = dto.BillingCycleId;
        entity.ContractDuration = dto.ContractDuration;
        entity.PricingModel = dto.PricingModel;
        entity.PlanStatus = dto.PlanStatus;
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
        entity.PlanStatus = "Inactive";
        entity.UpdatedBy = _currentUser.AccountId > 0 ? _currentUser.AccountId : null;
        entity.UpdatedDate = DateTime.UtcNow;
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
}
