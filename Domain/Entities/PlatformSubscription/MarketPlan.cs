using System;
using System.Collections.Generic;

public class MarketPlan
{
    public Guid PlanId { get; set; } = Guid.NewGuid();

    public string PlanName { get; set; } = "";
    public string TenantCategory { get; set; } = "";
    public string SettlementCurrency { get; set; } = "INR";
    public string BillingInterval { get; set; } = "Monthly";
    public string ContractValidity { get; set; } = "1 Year";
    public string PricingModel { get; set; } = "Flat";

    // Setup Fee
    public decimal InitialBasePrice { get; set; }

    // Recurring Fee
    public decimal AnnualMaintenanceCharge { get; set; }
    public decimal PlatformSubscriptionCharge { get; set; }

    // Pricing
    public decimal BasePrice { get; set; }
    public decimal MinimumPrice { get; set; }
    public string BillingCycle { get; set; } = "Monthly";
    public int UserLimit { get; set; } = 0;

    // Hardware restrictions
    public bool IsHardwareLocked { get; set; }

    // User Limit
    public int UserCreationLimit { get; set; } = 0;

    // Support
    public string SupportNumber { get; set; } = "";
    public string SupportEmail { get; set; } = "";
    public string InternalInstructions { get; set; } = "";

    // Admin Guard
    public bool AllowPriceChange { get; set; }
    public bool ForceSyncOnChange { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<PlanEntitlement> Entitlements { get; set; } = new();
    public List<PlanAddon> PlanAddons { get; set; } = new();
}
