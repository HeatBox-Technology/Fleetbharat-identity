using System;
using System.ComponentModel.DataAnnotations;

public record CreateAccountRequest(
    [Required, MaxLength(150)] string AccountName,
    [MaxLength(50)] string? AccountCode,
    [Required] int CategoryId,
    [Required, MaxLength(200)] string PrimaryDomain,
    [Required] int CountryId,
    int ParentAccountId,
    int userId,
    string HierarchyPath,
     int TaxTypeId,
    bool Status,
    string fullname = "",
     string email = "",
     string phone = "",
     string address = ""
);

public record UpdateAccountRequest(
    [Required, MaxLength(150)] string AccountName,
    [MaxLength(50)] string AccountCode,
    [Required] int CategoryId,
    [Required, MaxLength(200)] string PrimaryDomain,
    [Required] int CountryId,
     int ParentAccountId,
    int userId,
    string HierarchyPath,
     int TaxTypeId,
    bool Status,
     string fullname = "",
     string email = "",
     string phone = "",
     string address = ""
);

public class AccountResponseDto
{
    public int AccountId { get; set; }
    public string AccountCode { get; set; } = "";
    public string AccountName { get; set; } = "";
    public int? ParentAccountId { get; set; }
    public string HierarchyPath { get; set; } = "";
    public int Fk_userid { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = "";
    public string PrimaryDomain { get; set; } = "";
    public int CountryId { get; set; }
    public string CountryName { get; set; } = "";
    public string fullname { get; set; } = "";
    public string email { get; set; } = "";
    public string phone { get; set; } = "";
    public string address { get; set; } = "";
    public int TaxTypeId { get; set; }
    public bool Status { get; set; }

    public DateTime CreatedOn { get; set; }
}
