using System;
public class AddonMaster
{
    public Guid AddonId { get; set; } = Guid.NewGuid();

    public string AddonName { get; set; } = "";
    public string Category { get; set; } = "";     // SMS, MAP, API, STORAGE
    public string BillingType { get; set; } = "";  // PerUnit / Flat

    public decimal? PricePerUnit { get; set; }
    public decimal? FlatPrice { get; set; }
    public int? IncludedUnits { get; set; }

    public bool IsActive { get; set; } = true;
}
