using System;

public class mst_country
{
    public int CountryId { get; set; }
    public string Iso2Code { get; set; } = "IN";     // IN
    public string Iso3Code { get; set; } = "IND";      // IND
    public string CountryName { get; set; } = "India";   // India
    public string MobileDialCode { get; set; } = "+91";  // +91
    public string CurrencyCode { get; set; } = "INR";  // INR
    public string CurrencySymbol { get; set; } = "₹";  // ₹
    public string TimezoneName { get; set; } = "Asia/Kolkata";  // Asia/Kolkata
    public string UtcOffset { get; set; } = "+05:30";     // +05:30
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
