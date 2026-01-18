using System.ComponentModel.DataAnnotations;

public record CreateTaxTypeRequest(
    [Required] int CountryId,

    [Required, MaxLength(50)]
    string TaxTypeCode,

    [Required, MaxLength(100)]
    string TaxTypeName,

    bool IsActive
);

public record UpdateTaxTypeRequest(
    [Required] int CountryId,

    [Required, MaxLength(50)]
    string TaxTypeCode,

    [Required, MaxLength(100)]
    string TaxTypeName,

    bool IsActive
);

public class TaxTypeResponseDto
{
    public int TaxTypeId { get; set; }
    public int CountryId { get; set; }
    public string TaxTypeCode { get; set; } = "";
    public string TaxTypeName { get; set; } = "";
    public bool IsActive { get; set; }
}
