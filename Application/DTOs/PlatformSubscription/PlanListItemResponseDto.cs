using System;

public class PlanListItemResponseDto
{
    public Guid PlanId { get; set; }

    public string PlanName { get; set; } = "";
    public string TenantCategory { get; set; } = "";

    public PricingModelType PricingModel { get; set; }

    public decimal? InitialBasePrice { get; set; }   // only for Fixed

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}
