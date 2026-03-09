using System;

public class PlanFeature : IAccountEntity
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public int PlanId { get; set; }
    public string FeatureName { get; set; } = "";
    public bool IsEnabled { get; set; } = true;
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedDate { get; set; }

    public BillingPlan? Plan { get; set; }
}
