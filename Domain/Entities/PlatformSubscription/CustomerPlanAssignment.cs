using System;
public class CustomerPlanAssignment
{
    public Guid CustomerPlanAssignmentId { get; set; } = Guid.NewGuid();

    public Guid CustomerId { get; set; }
    public Guid PlanId { get; set; }

    public decimal? CustomPrice { get; set; }
    public int? CustomUserLimit { get; set; }
    public int? CustomVehicleLimit { get; set; }

    public string BillingCycle { get; set; } = "Monthly";

    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
