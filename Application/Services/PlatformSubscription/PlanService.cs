using System;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

public class PlanService : IPlanService
{
    private readonly IdentityDbContext _db;

    public PlanService(IdentityDbContext db)
    {
        _db = db;
    }

    public async Task<Guid> CreateAsync(CreateMarketPlanDto dto)
    {
        var plan = new MarketPlan
        {
            PlanName = dto.Structure.PlanName,
            TenantCategory = dto.Structure.TenantCategory,
            SettlementCurrency = dto.Structure.SettlementCurrency,
            BillingInterval = dto.Structure.BillingInterval,
            ContractValidity = dto.Structure.ContractValidity,
            PricingModel = dto.Structure.PricingModel,

            InitialBasePrice = dto.SetupFee.InitialBasePrice,
            AnnualMaintenanceCharge = dto.RecurringFee.AnnualMaintenanceCharge,
            PlatformSubscriptionCharge = dto.RecurringFee.PlatformSubscriptionCharge,

            IsHardwareLocked = dto.HardwareBinding.IsHardwareLocked,
            UserCreationLimit = dto.UserLimits.UserCreationLimit,

            SupportNumber = dto.Support.SupportNumber,
            SupportEmail = dto.Support.SupportEmail,
            InternalInstructions = dto.Support.InternalInstructions,

            BasePrice = dto.Pricing.BasePrice,
            MinimumPrice = dto.Pricing.MinimumPrice,
            BillingCycle = dto.Pricing.BillingCycle,
            UserLimit = dto.Pricing.UserLimit,

            AllowPriceChange = dto.AdminGuard.AllowPriceChange,
            ForceSyncOnChange = dto.AdminGuard.ForceSyncOnChange
        };

        _db.MarketPlans.Add(plan);

        // Save plan first
        await _db.SaveChangesAsync();

        // Save Feature mapping
        if (dto.FeatureIds != null && dto.FeatureIds.Any())
        {
            foreach (var featureId in dto.FeatureIds)
            {
                _db.PlanEntitlements.Add(new PlanEntitlement
                {
                    PlanId = plan.PlanId,
                    FeatureId = featureId
                });
            }
        }

        // Save Addon mapping
        if (dto.AddonIds != null && dto.AddonIds.Any())
        {
            foreach (var addonId in dto.AddonIds)
            {
                _db.PlanAddons.Add(new PlanAddon
                {
                    PlanId = plan.PlanId,
                    AddonId = addonId
                });
            }
        }

        await _db.SaveChangesAsync();
        return plan.PlanId;
    }

    public async Task<PlanDetailResponseDto?> GetByIdAsync(Guid planId)
    {
        var plan = await _db.MarketPlans.FirstOrDefaultAsync(x => x.PlanId == planId);
        if (plan == null) return null;

        var featureIds = await _db.PlanEntitlements
            .Where(x => x.PlanId == planId)
            .Select(x => x.FeatureId)
            .ToListAsync();

        var addonIds = await _db.PlanAddons
            .Where(x => x.PlanId == planId)
            .Select(x => x.AddonId)
            .ToListAsync();

        return new PlanDetailResponseDto
        {
            PlanId = plan.PlanId,
            Structure = new PlanStructuralDefinitionDto
            {
                PlanName = plan.PlanName,
                TenantCategory = plan.TenantCategory,
                SettlementCurrency = plan.SettlementCurrency,
                BillingInterval = plan.BillingInterval,
                ContractValidity = plan.ContractValidity,
                PricingModel = plan.PricingModel
            },
            SetupFee = new PlanSetupFeeDto { InitialBasePrice = plan.InitialBasePrice },
            RecurringFee = new PlanRecurringFeeDto
            {
                AnnualMaintenanceCharge = plan.AnnualMaintenanceCharge,
                PlatformSubscriptionCharge = plan.PlatformSubscriptionCharge
            },
            HardwareBinding = new PlanHardwareBindingDto
            {
                IsHardwareLocked = plan.IsHardwareLocked
            },
            UserLimits = new PlanUserLimitDto { UserCreationLimit = plan.UserCreationLimit },
            Support = new PlanSupportLineDto
            {
                SupportNumber = plan.SupportNumber,
                SupportEmail = plan.SupportEmail,
                InternalInstructions = plan.InternalInstructions
            },
            Pricing = new PlanPricingDto
            {
                BasePrice = plan.BasePrice,
                MinimumPrice = plan.MinimumPrice,
                BillingCycle = plan.BillingCycle,
                UserLimit = plan.UserLimit
            },
            AdminGuard = new PlanAdminGuardDto
            {
                AllowPriceChange = plan.AllowPriceChange,
                ForceSyncOnChange = plan.ForceSyncOnChange
            },
            FeatureIds = featureIds,
            AddonIds = addonIds,
            IsActive = plan.IsActive,
            CreatedAt = plan.CreatedAt
        };
    }
    public async Task<PagedResultDto<PlanListItemResponseDto>> GetPagedAsync(
     PagedRequestDto page, PlanFilterDto filter)
    {
        var query = _db.MarketPlans.AsQueryable();

        // Filters
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim().ToLower();
            query = query.Where(x => x.PlanName.ToLower().Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(filter.TenantCategory))
        {
            query = query.Where(x => x.TenantCategory == filter.TenantCategory);
        }

        if (!string.IsNullOrWhiteSpace(filter.BillingCycle))
        {
            query = query.Where(x => x.BillingCycle == filter.BillingCycle);
        }

        if (!string.IsNullOrWhiteSpace(filter.PricingModel))
        {
            query = query.Where(x => x.PricingModel == filter.PricingModel);
        }

        if (filter.IsActive.HasValue)
        {
            query = query.Where(x => x.IsActive == filter.IsActive.Value);
        }

        // Total count (before paging)
        var total = await query.CountAsync();

        // Data paging + sort
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .Select(x => new PlanListItemResponseDto
            {
                PlanId = x.PlanId,
                PlanName = x.PlanName,
                TenantCategory = x.TenantCategory,
                BillingCycle = x.BillingCycle,
                PricingModel = x.PricingModel,
                BasePrice = x.BasePrice,
                MinimumPrice = x.MinimumPrice,
                IsActive = x.IsActive,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();

        return new PagedResultDto<PlanListItemResponseDto>
        {
            Page = page.Page,
            PageSize = page.PageSize,
            TotalRecords = total,
            TotalPages = (int)Math.Ceiling(total / (double)page.PageSize),
            Items = items
        };
    }
    public async Task<bool> UpdateAsync(Guid planId, CreateMarketPlanDto dto)
    {
        var plan = await _db.MarketPlans.FirstOrDefaultAsync(x => x.PlanId == planId);
        if (plan == null) return false;

        plan.PlanName = dto.Structure.PlanName;
        plan.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(Guid planId)
    {
        var plan = await _db.MarketPlans.FirstOrDefaultAsync(x => x.PlanId == planId);
        if (plan == null) return false;

        _db.MarketPlans.Remove(plan);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateStatusAsync(Guid planId, bool isActive)
    {
        var plan = await _db.MarketPlans.FirstOrDefaultAsync(x => x.PlanId == planId);
        if (plan == null) return false;

        plan.IsActive = isActive;
        plan.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return true;
    }

}
