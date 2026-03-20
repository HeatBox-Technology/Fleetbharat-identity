using System;

public class PlanEntitlement
{
    public Guid PlanEntitlementId { get; set; } = Guid.NewGuid();

    public Guid PlanId { get; set; }
    public MarketPlan? Plan { get; set; }

    public Guid FeatureId { get; set; }
    public FeatureMaster? Feature { get; set; }
}
