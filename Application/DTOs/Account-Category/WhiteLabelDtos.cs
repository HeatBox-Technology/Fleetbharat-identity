using System;
using System.ComponentModel.DataAnnotations;

public class CreateWhiteLabelRequest
{
    [Required]
    public int AccountId { get; set; }

    [Required]
    public string CustomEntryFqdn { get; set; } = "";   // portal.partner.com

    public string? LogoUrl { get; set; }                // url after upload
    public string PrimaryColorHex { get; set; } = "#4F46E5";
    public string? SecondaryColorHex { get; set; } = null;

    public bool IsActive { get; set; } = true;
}

public class UpdateWhiteLabelRequest
{
    [Required]
    public string CustomEntryFqdn { get; set; } = "";

    public string? LogoUrl { get; set; }
    public string PrimaryColorHex { get; set; } = "#4F46E5";
    public string? SecondaryColorHex { get; set; } = null;

    public bool IsActive { get; set; } = true;
}

public class WhiteLabelResponseDto
{
    public int WhiteLabelId { get; set; }

    public int AccountId { get; set; }
    public string AccountName { get; set; } = "";

    public string CustomEntryFqdn { get; set; } = "";
    public string? LogoUrl { get; set; }
    public string PrimaryColorHex { get; set; } = "";
    public string? SecondaryColorHex { get; set; } = null;

    public bool IsActive { get; set; }

    public DateTime CreatedOn { get; set; }
    public DateTime? UpdatedOn { get; set; }
}
