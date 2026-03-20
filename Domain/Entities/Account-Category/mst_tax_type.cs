using System;

public class mst_tax_type
{
    public int TaxTypeId { get; set; }
    public int CountryId { get; set; }            // FK Country
    public string TaxTypeCode { get; set; } = ""; // GST/VAT
    public string TaxTypeName { get; set; } = ""; // GST India / VAT UAE

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
