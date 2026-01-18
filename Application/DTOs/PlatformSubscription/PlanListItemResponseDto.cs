using System;

public class PlanListItemResponseDto
{
    public Guid PlanId { get; set; }

    public string PlanName { get; set; } = "";
    public string TenantCategory { get; set; } = "";

    public string BillingCycle { get; set; } = "";
    public string PricingModel { get; set; } = "";

    public decimal BasePrice { get; set; }
    public decimal MinimumPrice { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}