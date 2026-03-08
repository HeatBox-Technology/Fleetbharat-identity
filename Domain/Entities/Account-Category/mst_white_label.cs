using System;

public class mst_white_label : IAccountEntity
{
    public int WhiteLabelId { get; set; }
    public int AccountId { get; set; }                   // FK mst_account
    public string? BrandName { get; set; }
    public string? LogoName { get; set; }
    public string? LogoPath { get; set; }
    public string CustomEntryFqdn { get; set; } = "";    // portal.partner.com
    public string? LogoUrl { get; set; }                 // uploaded image url (S3/local)
    public string PrimaryColorHex { get; set; } = "#843811";
    public string? SecondaryColorHex { get; set; } = null;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedOn { get; set; }
    public bool IsDeleted { get; set; } = false;
}
