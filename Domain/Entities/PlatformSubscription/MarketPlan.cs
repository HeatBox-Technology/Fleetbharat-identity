using System;
using System.Collections.Generic;


public class MarketPlan
{
    public Guid PlanId { get; set; } = Guid.NewGuid();

    public string PlanName { get; set; } = "";
    public int Fk_CategoryID { get; set; }

    public string TenantCategory { get; set; } = "";
    public int Fk_CurrencyId { get; set; }
    public string SettlementCurrency { get; set; } = "INR";
    public string BillingInterval { get; set; } = "Monthly";
    public string ContractValidity { get; set; } = "1 Year";

    public PricingModelType PricingModel { get; set; }

    // only for Fixed
    public decimal? InitialBasePrice { get; set; }

    // recurring (both)
    public decimal AnnualMaintenanceCharge { get; set; }
    public decimal PlatformSubscriptionCharge { get; set; }

    public bool IsHardwareLocked { get; set; }

    public int UserCreationLimit { get; set; }


    public string SupportNumber { get; set; } = "";
    public string SupportEmail { get; set; } = "";
    public string InternalInstructions { get; set; } = "";

    public bool AllowPriceChange { get; set; }
    public bool ForceSyncOnChange { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<PlanUnitLicense> UnitLicenses { get; set; } = new();
    public List<PlanEntitlement> Entitlements { get; set; } = new();
    public List<PlanAddon> PlanAddons { get; set; } = new();
    public List<PlanEntitlementModule> EntitlementModules { get; set; } = new();
}
