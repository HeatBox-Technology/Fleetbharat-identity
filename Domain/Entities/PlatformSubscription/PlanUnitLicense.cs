using System;

public class PlanUnitLicense
{
    public Guid PlanUnitLicenseId { get; set; } = Guid.NewGuid();

    public Guid PlanId { get; set; }
    public Guid FeatureId { get; set; }

    public decimal UnitPrice { get; set; }

    public bool IsActive { get; set; } = true;

    public MarketPlan Plan { get; set; }
}