using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

public record CreateAccountRequest
(
    [Required, MaxLength(150)] string AccountName,

    [MaxLength(50)] string? AccountCode,

    [Required] int CategoryId,

    [MaxLength(200)] string PrimaryDomain,

    [Required] int CountryId,

   [Required] string StateId,
   [Required] string CityId,
     string Zipcode,

    int? ParentAccountId,

    string RefferCode,

    [Required] int TaxTypeId,

    bool Status,

    // contact person
    [Required] string fullname,
   [Required] string email,
   [Required] string phone,
    string Position,

    // address
    string address,

   // business profile
   [Required] string BusinessPhone,
    [Required] string BusinessEmail,
    [Required] string BusinessAddress,
    string BusinessHours,
    string BusinessTimeZone,

    // user access
    [Required] string UserName,
     string Password,

    string share,

    int userId

);

public record UpdateAccountRequest
(
    [Required, MaxLength(150)] string AccountName,

    [MaxLength(50)] string? AccountCode,

    [Required] int CategoryId,

    [Required, MaxLength(200)] string PrimaryDomain,

    [Required] int CountryId,

   [Required] string StateId,
    [Required] string CityId,
    string Zipcode,

    int? ParentAccountId,

    string RefferCode,

    [Required] int TaxTypeId,

    bool Status,

   // contact person
   [Required] string fullname,
    [Required] string email,
    [Required] string phone,
    string Position,

    // address
    string address,

   // business profile
   [Required] string BusinessPhone,
    [Required] string BusinessEmail,
    [Required] string BusinessAddress,
    string BusinessHours,
    string BusinessTimeZone,

   // user access
   [Required] string UserName,
    string? Password,

    string share,

    int userId
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
    public string UserName { get; set; } = "";
    public string ContactPersonName { get; set; } = "";
    public string ContactPersonEmail { get; set; } = "";
    public string ContactPersonPhone { get; set; } = "";
    public string SuperiorName { get; set; } = "";
    public string Reffer { get; set; } = "";
    public string PrimaryDomain { get; set; } = "";

    public string CountryName { get; set; } = "";
    public string StateName { get; set; } = "";
    public string CityName { get; set; } = "";
    public int CountryId { get; set; } = 0;
    public string StateId { get; set; } = "";
    public string CityId { get; set; } = "";

    public string fullname { get; set; } = "";
    public string email { get; set; } = "";
    public string phone { get; set; } = "";
    public string address { get; set; } = "";
    public int TaxTypeId { get; set; }
    public bool Status { get; set; }
    public string Position { get; set; } = "";
    public string BusinessPhone { get; set; } = "";
    public string BusinessEmail { get; set; } = "";
    public string BusinessAddress { get; set; } = "";
    public string BusinessHours { get; set; } = "";
    public string BusinessTimeZone { get; set; } = "";
    public string share { get; set; } = "";
    public string Zipcode { get; set; } = "";
    public string usernamesacc { get; set; } = "";
    public string password { get; set; } = "";
    public DateTime CreatedOn { get; set; }
}

public class AccountCardCountDto
{
    public int Total { get; set; }
    public int Active { get; set; }
    public int Pending { get; set; }
    public int Inactive { get; set; }
}

public class AccountListWithCardDto
{
    public PagedResultDto<AccountResponseDto> PageData { get; set; } = new();
    public AccountCardCountDto CardCounts { get; set; } = new();
}

public class AccountHierarchyDto
{
    public int AccountId { get; set; }
    public string AccountName { get; set; } = "";
    public string AccountCode { get; set; } = "";
    public string CategoryName { get; set; } = "";
    public bool Status { get; set; }

    public List<AccountHierarchyDto> Children { get; set; } = new();
}
