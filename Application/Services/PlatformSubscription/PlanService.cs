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
        // validation
        if (dto.Structure.PricingModel == PricingModelType.Fixed &&
            dto.SetupFee.InitialBasePrice == null)
            throw new Exception("Initial base price is required for Fixed pricing.");

        if (dto.Structure.PricingModel == PricingModelType.LicenseBased &&
            dto.UnitLicenses == null)
            throw new Exception("Unit licenses are required for License based pricing.");

        var plan = new MarketPlan
        {
            PlanName = dto.Structure.PlanName,
            TenantCategory = dto.Structure.TenantCategory,
            SettlementCurrency = dto.Structure.SettlementCurrency,
            BillingInterval = dto.Structure.BillingInterval,
            ContractValidity = dto.Structure.ContractValidity,
            PricingModel = dto.Structure.PricingModel,

            InitialBasePrice = dto.Structure.PricingModel == PricingModelType.Fixed
                                ? dto.SetupFee.InitialBasePrice
                                : null,

            AnnualMaintenanceCharge = dto.RecurringFee.AnnualMaintenanceCharge,
            PlatformSubscriptionCharge = dto.RecurringFee.PlatformSubscriptionCharge,

            IsHardwareLocked = dto.HardwareBinding.IsHardwareLocked,
            UserCreationLimit = dto.UserLimits.UserCreationLimit,

            SupportNumber = dto.Support.SupportNumber,
            SupportEmail = dto.Support.SupportEmail,
            InternalInstructions = dto.Support.InternalInstructions,

            AllowPriceChange = dto.AdminGuard.AllowPriceChange,
            ForceSyncOnChange = dto.AdminGuard.ForceSyncOnChange
        };

        _db.MarketPlans.Add(plan);
        await _db.SaveChangesAsync();

        // unit licenses
        if (dto.Structure.PricingModel == PricingModelType.LicenseBased &&
            dto.UnitLicenses != null)
        {
            foreach (var item in dto.UnitLicenses)
            {
                _db.PlanUnitLicenses.Add(new PlanUnitLicense
                {
                    PlanId = plan.PlanId,
                    FeatureId = item.FeatureId,
                    UnitPrice = item.UnitPrice
                });
            }
        }

        // features
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

        // addons
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

        List<PlanUnitLicenseDto>? unitLicenses = null;

        if (plan.PricingModel == PricingModelType.LicenseBased)
        {
            unitLicenses = await _db.PlanUnitLicenses
                .Where(x => x.PlanId == planId && x.IsActive)
                .Select(x => new PlanUnitLicenseDto
                {
                    FeatureId = x.FeatureId,
                    UnitPrice = x.UnitPrice
                })
                .ToListAsync();
        }

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

            SetupFee = new PlanSetupFeeDto
            {
                InitialBasePrice = plan.InitialBasePrice
            },

            RecurringFee = new PlanRecurringFeeDto
            {
                AnnualMaintenanceCharge = plan.AnnualMaintenanceCharge,
                PlatformSubscriptionCharge = plan.PlatformSubscriptionCharge
            },

            HardwareBinding = new PlanHardwareBindingDto
            {
                IsHardwareLocked = plan.IsHardwareLocked
            },

            UserLimits = new PlanUserLimitDto
            {
                UserCreationLimit = plan.UserCreationLimit
            },

            Support = new PlanSupportLineDto
            {
                SupportNumber = plan.SupportNumber,
                SupportEmail = plan.SupportEmail,
                InternalInstructions = plan.InternalInstructions
            },

            AdminGuard = new PlanAdminGuardDto
            {
                AllowPriceChange = plan.AllowPriceChange,
                ForceSyncOnChange = plan.ForceSyncOnChange
            },

            UnitLicenses = unitLicenses,
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

        if (filter.PricingModel.HasValue)
        {
            query = query.Where(x => x.PricingModel == filter.PricingModel.Value);
        }

        if (filter.IsActive.HasValue)
        {
            query = query.Where(x => x.IsActive == filter.IsActive.Value);
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .Select(x => new PlanListItemResponseDto
            {
                PlanId = x.PlanId,
                PlanName = x.PlanName,
                TenantCategory = x.TenantCategory,
                PricingModel = x.PricingModel,
                InitialBasePrice = x.PricingModel == PricingModelType.Fixed
                                        ? x.InitialBasePrice
                                        : null,
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
        var plan = await _db.MarketPlans
            .Include(x => x.UnitLicenses)
            .FirstOrDefaultAsync(x => x.PlanId == planId);

        if (plan == null)
            return false;

        // ---------------------------
        // basic validation
        // ---------------------------
        if (dto.Structure.PricingModel == PricingModelType.Fixed &&
            dto.SetupFee.InitialBasePrice == null)
            throw new Exception("Initial base price is required for Fixed pricing.");

        if (dto.Structure.PricingModel == PricingModelType.LicenseBased &&
            (dto.UnitLicenses == null || !dto.UnitLicenses.Any()))
            throw new Exception("Unit licenses are required for License based pricing.");

        // ---------------------------
        // update main fields
        // ---------------------------
        plan.PlanName = dto.Structure.PlanName;
        plan.TenantCategory = dto.Structure.TenantCategory;
        plan.SettlementCurrency = dto.Structure.SettlementCurrency;
        plan.BillingInterval = dto.Structure.BillingInterval;
        plan.ContractValidity = dto.Structure.ContractValidity;
        plan.PricingModel = dto.Structure.PricingModel;

        plan.InitialBasePrice =
            dto.Structure.PricingModel == PricingModelType.Fixed
                ? dto.SetupFee.InitialBasePrice
                : null;

        plan.AnnualMaintenanceCharge = dto.RecurringFee.AnnualMaintenanceCharge;
        plan.PlatformSubscriptionCharge = dto.RecurringFee.PlatformSubscriptionCharge;

        plan.IsHardwareLocked = dto.HardwareBinding.IsHardwareLocked;
        plan.UserCreationLimit = dto.UserLimits.UserCreationLimit;

        plan.SupportNumber = dto.Support.SupportNumber;
        plan.SupportEmail = dto.Support.SupportEmail;
        plan.InternalInstructions = dto.Support.InternalInstructions;

        plan.AllowPriceChange = dto.AdminGuard.AllowPriceChange;
        plan.ForceSyncOnChange = dto.AdminGuard.ForceSyncOnChange;

        plan.UpdatedAt = DateTime.UtcNow;

        // ---------------------------
        // update unit licenses
        // ---------------------------
        var existingUnitLicenses = await _db.PlanUnitLicenses
            .Where(x => x.PlanId == planId)
            .ToListAsync();

        _db.PlanUnitLicenses.RemoveRange(existingUnitLicenses);

        if (dto.Structure.PricingModel == PricingModelType.LicenseBased &&
            dto.UnitLicenses != null)
        {
            foreach (var ul in dto.UnitLicenses)
            {
                _db.PlanUnitLicenses.Add(new PlanUnitLicense
                {
                    PlanId = planId,
                    FeatureId = ul.FeatureId,
                    UnitPrice = ul.UnitPrice
                });
            }
        }

        // ---------------------------
        // update feature mappings
        // ---------------------------
        var existingFeatures = await _db.PlanEntitlements
            .Where(x => x.PlanId == planId)
            .ToListAsync();

        _db.PlanEntitlements.RemoveRange(existingFeatures);

        if (dto.FeatureIds != null)
        {
            foreach (var fid in dto.FeatureIds)
            {
                _db.PlanEntitlements.Add(new PlanEntitlement
                {
                    PlanId = planId,
                    FeatureId = fid
                });
            }
        }

        // ---------------------------
        // update addon mappings
        // ---------------------------
        var existingAddons = await _db.PlanAddons
            .Where(x => x.PlanId == planId)
            .ToListAsync();

        _db.PlanAddons.RemoveRange(existingAddons);

        if (dto.AddonIds != null)
        {
            foreach (var aid in dto.AddonIds)
            {
                _db.PlanAddons.Add(new PlanAddon
                {
                    PlanId = planId,
                    AddonId = aid
                });
            }
        }

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
