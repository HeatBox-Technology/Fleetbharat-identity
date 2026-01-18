
public class PlanFilterDto
{
    public string? Search { get; set; }          // search by name/code
    public string? TenantCategory { get; set; }  // End User / Distributor / Reseller
    public string? BillingCycle { get; set; }    // Monthly / Yearly
    public bool? IsActive { get; set; }          // true/false
    public string? PricingModel { get; set; }    // Flat / Hybrid / Consumption
}
