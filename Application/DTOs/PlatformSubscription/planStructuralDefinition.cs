public class PlanStructuralDefinitionDto
{
    public string PlanName { get; set; }
    public string TenantCategory { get; set; } // End User / Distributor / Reseller
    public string SettlementCurrency { get; set; }
    public string BillingInterval { get; set; } // Monthly, Quarterly, Yearly
    public string ContractValidity { get; set; } // e.g., "1 Year"
    public string PricingModel { get; set; } // Fixed / Consumption / Hybrid
}