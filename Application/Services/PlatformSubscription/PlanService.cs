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
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        // --------------------
        // Validation
        // --------------------

        if (dto.Structure.PricingModel == PricingModelType.Fixed)
        {
            if (dto.SetupFee == null || dto.SetupFee.InitialBasePrice == null)
                throw new Exception("Initial base price is required for Fixed pricing.");

            if (dto.UnitLicenses != null && dto.UnitLicenses.Any())
                throw new Exception("Unit licenses are not allowed for Fixed pricing.");
        }

        if (dto.Structure.PricingModel == PricingModelType.LicenseBased)
        {
            if (dto.UnitLicenses == null || !dto.UnitLicenses.Any())
                throw new Exception("Unit licenses are required for License based pricing.");

            if (dto.SetupFee != null)
                throw new Exception("Setup fee is not allowed for License based pricing.");
        }

        // --------------------
        // Market plan
        // --------------------

        var plan = new MarketPlan
        {
            PlanName = dto.Structure.PlanName,
            Fk_CategoryID = dto.Structure.CategoryID,
            TenantCategory = dto.Structure.TenantCategory,
            Fk_CurrencyId = dto.Structure.CurrencyId,
            SettlementCurrency = dto.Structure.SettlementCurrency,
            BillingInterval = dto.Structure.BillingInterval,
            ContractValidity = dto.Structure.ContractValidity,
            PricingModel = dto.Structure.PricingModel,
            InitialBasePrice = dto.Structure.PricingModel == PricingModelType.Fixed
                                    ? dto.SetupFee!.InitialBasePrice
                                    : null,

            AnnualMaintenanceCharge = dto.RecurringFee?.AnnualMaintenanceCharge ?? 0,
            PlatformSubscriptionCharge = dto.RecurringFee?.PlatformSubscriptionCharge ?? 0,

            IsHardwareLocked = dto.HardwareBinding?.IsHardwareLocked ?? false,
            UserCreationLimit = dto.UserLimits?.UserCreationLimit ?? 0,

            SupportNumber = dto.Support?.SupportNumber ?? "",
            SupportEmail = dto.Support?.SupportEmail ?? "",
            InternalInstructions = dto.Support?.InternalInstructions ?? "",

            AllowPriceChange = dto.AdminGuard?.AllowPriceChange ?? false,
            ForceSyncOnChange = dto.AdminGuard?.ForceSyncOnChange ?? false
        };

        _db.MarketPlans.Add(plan);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            var msg = ex.InnerException?.Message;
            throw new Exception(msg, ex);
        }

        // --------------------
        // Unit licenses
        // --------------------

        if (dto.Structure.PricingModel == PricingModelType.LicenseBased)
        {
            foreach (var item in dto.UnitLicenses!)
            {
                _db.PlanUnitLicenses.Add(new PlanUnitLicense
                {
                    PlanId = plan.PlanId,
                    FeatureId = item.FeatureId,
                    UnitPrice = item.UnitPrice
                });
            }
        }

        // --------------------
        // Entitlement Matrix (modules)
        // --------------------

        if (dto.EntitlementModuleIds != null && dto.EntitlementModuleIds.Any())
        {
            foreach (var moduleId in dto.EntitlementModuleIds)
            {
                _db.EntitlementModules.Add(new PlanEntitlementModule
                {
                    PlanId = plan.PlanId,
                    FormModuleId = moduleId
                });
            }
        }

        // --------------------
        // Features
        // --------------------

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

        // --------------------
        // Addons
        // --------------------

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

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            var msg = ex.InnerException?.Message;
            throw new Exception(msg, ex);
        }

        return plan.PlanId;
    }

    public async Task<PlanDetailResponseDto?> GetByIdAsync(Guid planId)
    {
        var plan = await _db.MarketPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.PlanId == planId);

        if (plan == null)
            return null;

        var featureIds = await _db.PlanEntitlements
            .Where(x => x.PlanId == planId)
            .Select(x => x.FeatureId)
            .ToListAsync();

        var addonIds = await _db.PlanAddons
            .Where(x => x.PlanId == planId)
            .Select(x => x.AddonId)
            .ToListAsync();

        // ✅ Entitlement Matrix (Form modules)
        var entitlementModuleIds = await _db.EntitlementModules
            .Where(x => x.PlanId == planId)
            .Select(x => x.FormModuleId)
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
                CategoryID = plan.Fk_CategoryID,
                TenantCategory = plan.TenantCategory,
                CurrencyId = plan.Fk_CurrencyId,
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

            // existing
            FeatureIds = featureIds,
            AddonIds = addonIds,

            // ✅ new
            EntitlementModuleIds = entitlementModuleIds,

            IsActive = plan.IsActive,
            CreatedAt = plan.CreatedAt
        };
    }

    public async Task<PagedResultDto<PlanListItemResponseDto>> GetPagedAsync(
     PagedRequestDto page, PlanFilterDto filter)
    {
        var query = _db.MarketPlans.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            query = query.Where(x =>
                EF.Functions.ILike(x.PlanName, $"%{search}%"));
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

        // ✅ filter by entitlement module
        if (filter.FormModuleId.HasValue)
        {
            var moduleId = filter.FormModuleId.Value;

            query = query.Where(p =>
                _db.EntitlementModules.Any(m =>
                    m.PlanId == p.PlanId &&
                    m.FormModuleId == moduleId));
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
                CategoryID = x.Fk_CategoryID,
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
            .FirstOrDefaultAsync(x => x.PlanId == planId);

        if (plan == null)
            return false;

        // ---------------------------
        // validation
        // ---------------------------

        if (dto.Structure.PricingModel == PricingModelType.Fixed)
        {
            if (dto.SetupFee == null || dto.SetupFee.InitialBasePrice == null)
                throw new Exception("Initial base price is required for Fixed pricing.");

            if (dto.UnitLicenses != null && dto.UnitLicenses.Any())
                throw new Exception("Unit licenses are not allowed for Fixed pricing.");
        }

        if (dto.Structure.PricingModel == PricingModelType.LicenseBased)
        {
            if (dto.UnitLicenses == null || !dto.UnitLicenses.Any())
                throw new Exception("Unit licenses are required for License based pricing.");

            if (dto.SetupFee != null)
                throw new Exception("Setup fee is not allowed for License based pricing.");
        }

        // ---------------------------
        // update main fields
        // ---------------------------

        plan.PlanName = dto.Structure.PlanName;
        plan.Fk_CategoryID = dto.Structure.CategoryID;
        plan.TenantCategory = dto.Structure.TenantCategory;

        // remove this if you don't really have it in entity
        // plan.Fk_CurrencyId = dto.Structure.CurrencyId;

        plan.SettlementCurrency = dto.Structure.SettlementCurrency;
        plan.BillingInterval = dto.Structure.BillingInterval;
        plan.ContractValidity = dto.Structure.ContractValidity;
        plan.PricingModel = dto.Structure.PricingModel;

        plan.InitialBasePrice =
            dto.Structure.PricingModel == PricingModelType.Fixed
                ? dto.SetupFee!.InitialBasePrice
                : null;

        plan.AnnualMaintenanceCharge = dto.RecurringFee?.AnnualMaintenanceCharge ?? 0;
        plan.PlatformSubscriptionCharge = dto.RecurringFee?.PlatformSubscriptionCharge ?? 0;

        plan.IsHardwareLocked = dto.HardwareBinding?.IsHardwareLocked ?? false;
        plan.UserCreationLimit = dto.UserLimits?.UserCreationLimit ?? 0;

        plan.SupportNumber = dto.Support?.SupportNumber ?? "";
        plan.SupportEmail = dto.Support?.SupportEmail ?? "";
        plan.InternalInstructions = dto.Support?.InternalInstructions ?? "";

        plan.AllowPriceChange = dto.AdminGuard?.AllowPriceChange ?? false;
        plan.ForceSyncOnChange = dto.AdminGuard?.ForceSyncOnChange ?? false;

        plan.UpdatedAt = DateTime.UtcNow;

        // ---------------------------
        // unit licenses
        // ---------------------------

        var existingUnitLicenses = await _db.PlanUnitLicenses
            .Where(x => x.PlanId == planId)
            .ToListAsync();

        _db.PlanUnitLicenses.RemoveRange(existingUnitLicenses);

        if (dto.Structure.PricingModel == PricingModelType.LicenseBased)
        {
            foreach (var ul in dto.UnitLicenses!)
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
        // entitlement matrix (modules)
        // ---------------------------

        var existingModules = await _db.EntitlementModules
            .Where(x => x.PlanId == planId)
            .ToListAsync();

        _db.EntitlementModules.RemoveRange(existingModules);

        if (dto.EntitlementModuleIds != null && dto.EntitlementModuleIds.Any())
        {
            foreach (var moduleId in dto.EntitlementModuleIds)
            {
                _db.EntitlementModules.Add(new PlanEntitlementModule
                {
                    PlanId = planId,
                    FormModuleId = moduleId
                });
            }
        }

        // ---------------------------
        // feature mappings
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
        // addon mappings
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
