public class PlanPricingDto
{
    public decimal BasePrice { get; set; }
    public decimal MinimumPrice { get; set; }
    public string BillingCycle { get; set; } // Monthly, Yearly
    public int UserLimit { get; set; } // Unlimited = 0
}