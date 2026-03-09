using System;
using System.Collections.Generic;

public class CreatePlanDto
{
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
    public List<int> SolutionIds { get; set; } = new();
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
}

public class UpdatePlanDto : CreatePlanDto
{
}

public class PlanResponseDto
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public string PlanName { get; set; } = "";
    public string? Description { get; set; }
    public int PlanCategoryId { get; set; }
    public int CurrencyId { get; set; }
    public int BillingCycleId { get; set; }
    public int ContractDuration { get; set; }
    public string PricingModel { get; set; } = "";
    public string PlanStatus { get; set; } = "";
    public int TierId { get; set; }
    public decimal BaseRate { get; set; }
    public int MinUnits { get; set; }
    public int MaxUnits { get; set; }
    public decimal LicensePricePerUnit { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal RecurringPlatformFee { get; set; }
    public decimal RecurringAmcFee { get; set; }
    public List<int> SolutionIds { get; set; } = new();
    public List<string> SolutionNames { get; set; } = new();
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
}

public class PlanFeatureUpsertDto
{
    public int AccountId { get; set; }
    public List<PlanFeatureItemDto> Features { get; set; } = new();
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
}

public class PlanFeatureItemDto
{
    public string FeatureName { get; set; } = "";
    public bool IsEnabled { get; set; } = true;
}

public class AccountSubscriptionMapPlanDto
{
    public int AccountId { get; set; }
    public int PlanId { get; set; }
    public int Units { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = "Active";
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
}

public class AccountSubscriptionResponseDto
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public int PlanId { get; set; }
    public int Units { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = "";
    public DateTime NextBillingDate { get; set; }
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
}

public class UsageRecordCreateDto
{
    public int AccountId { get; set; }
    public int SubscriptionId { get; set; }
    public string UsageType { get; set; } = "";
    public decimal UnitsConsumed { get; set; }
    public DateTime UsageDate { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
}

public class UsageRecordResponseDto
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public int SubscriptionId { get; set; }
    public string UsageType { get; set; } = "";
    public decimal UnitsConsumed { get; set; }
    public DateTime UsageDate { get; set; }
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
}

public class InvoiceManualCreateDto
{
    public int AccountId { get; set; }
    public int SubscriptionId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "INR";
    public DateTime? InvoiceDate { get; set; }
    public DateTime? DueDate { get; set; }
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
}

public class InvoiceResponseDto
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public string InvoiceNumber { get; set; } = "";
    public int SubscriptionId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "";
    public string Status { get; set; } = "";
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
}

public class BillingRevenueDto
{
    public string Month { get; set; } = "";
    public decimal RevenueAmount { get; set; }
}

public class BillingMarketPenetrationDto
{
    public string Region { get; set; } = "";
    public decimal Percentage { get; set; }
}
