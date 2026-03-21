using System;
using System.Collections.Generic;

public class BillingPlan : IAccountEntity
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public string PlanName { get; set; } = "";
    public string? Description { get; set; }
    public int PlanCategoryId { get; set; }
    public int CurrencyId { get; set; }
    public int BillingCycleId { get; set; }
    public int ContractDuration { get; set; }
    public string PricingModel { get; set; } = "Fixed";
    public string PlanStatus { get; set; } = "Active";
    public int TierId { get; set; }
    public decimal BaseRate { get; set; }
    public int MinUnits { get; set; }
    public int MaxUnits { get; set; }
    public decimal LicensePricePerUnit { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal RecurringPlatformFee { get; set; }
    public decimal RecurringAmcFee { get; set; }
    public bool IsActive { get; set; } = true;
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedDate { get; set; }
    public int? DeletedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
    public bool IsDeleted { get; set; }

    public List<PlanFeature> Features { get; set; } = new();
    public List<PlanSolution> Solutions { get; set; } = new();
}
