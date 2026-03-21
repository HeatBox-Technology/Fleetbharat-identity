using System;

public class AccountSubscription : IAccountEntity
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public int PlanId { get; set; }
    public int Units { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = "Active";
    public DateTime NextBillingDate { get; set; }
    public bool IsActive { get; set; } = true;
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedDate { get; set; }
    public int? DeletedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
    public bool IsDeleted { get; set; }

    public BillingPlan? Plan { get; set; }
}
