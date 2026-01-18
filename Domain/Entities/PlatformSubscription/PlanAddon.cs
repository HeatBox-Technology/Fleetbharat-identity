using System;
public class PlanAddon
{
    public Guid PlanAddonId { get; set; } = Guid.NewGuid();

    public Guid PlanId { get; set; }
    public MarketPlan? Plan { get; set; }

    public Guid AddonId { get; set; }
    public AddonMaster? Addon { get; set; }
}
