using System;

public class PlanSolution : IAccountEntity
{
    public int Id { get; set; }
    public int PlanId { get; set; }
    public int SolutionId { get; set; }
    public int AccountId { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;

    public BillingPlan? Plan { get; set; }
    public SolutionMaster? Solution { get; set; }
}
