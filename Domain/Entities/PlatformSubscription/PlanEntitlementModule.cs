using System;

public class PlanEntitlementModule
{
    public Guid PlanId { get; set; }

    public MarketPlan MarketPlan { get; set; } = null!;

    public int FormModuleId { get; set; }

    // optional navigation
    //public FormModule FormModule { get; set; } = null!;
}
