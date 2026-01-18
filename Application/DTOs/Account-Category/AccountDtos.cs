using System;
using System.ComponentModel.DataAnnotations;

public record CreateAccountRequest(
    [Required, MaxLength(150)] string AccountName,
    [MaxLength(50)] string? AccountCode,
    [Required] int CategoryId,
    [Required, MaxLength(200)] string PrimaryDomain,
    [Required] int CountryId,
     int TaxTypeId,
    bool Status
);

public record UpdateAccountRequest(
    [Required, MaxLength(150)] string AccountName,
    [MaxLength(50)] string AccountCode,
    [Required] int CategoryId,
    [Required, MaxLength(200)] string PrimaryDomain,
    [Required] int CountryId,
     int TaxTypeId,
    bool Status
);

public class AccountResponseDto
{
    public int AccountId { get; set; }
    public string AccountCode { get; set; } = "";
    public string AccountName { get; set; } = "";
    public int CategoryId { get; set; }
    public string PrimaryDomain { get; set; } = "";
    public int CountryId { get; set; }
    public int TaxTypeId { get; set; }
    public bool Status { get; set; }
    public DateTime CreatedOn { get; set; }
}
