public class PlanAddonDto
{
    public string AddonName { get; set; } = "";
    public string Category { get; set; } // SMS, MAP, API, STORAGE
    public string BillingType { get; set; } // PerUnit / Flat
    public decimal? PricePerUnit { get; set; }
    public decimal? FlatPrice { get; set; }
    public int? IncludedUnits { get; set; }
}