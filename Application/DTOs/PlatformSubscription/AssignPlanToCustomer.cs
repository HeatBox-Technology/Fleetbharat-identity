using System;
using System.Collections.Generic;

public class AssignPlanToCustomerDto
{
    public Guid CustomerId { get; set; }
    public Guid PlanId { get; set; }
    public List<Guid>? AddonIds { get; set; }
    public decimal? CustomPrice { get; set; }
    public int? CustomUserLimit { get; set; }
    public int? CustomVehicleLimit { get; set; }
    public string BillingCycle { get; set; }
    public DateTime StartDate { get; set; }
}