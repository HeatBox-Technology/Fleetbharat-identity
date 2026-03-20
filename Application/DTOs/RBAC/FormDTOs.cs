using System.ComponentModel.DataAnnotations;

public record CreateFormRequest(
    [Required, MaxLength(50)] string FormCode,
    [Required, MaxLength(150)] string FormName,
    [Required, MaxLength(50)] string ModuleName,
    [Required, MaxLength(200)] string PageUrl,
    string? IconName,
    int SortOrder,
    bool IsMenu,
    bool IsBulk,
    bool IsVisible,
    bool IsActive,
    int? FormModuleId = null,
    string? FilterConfigJson = null
);

public record UpdateFormRequest(
    [Required, MaxLength(50)] string FormCode,
    [Required, MaxLength(150)] string FormName,
    [Required, MaxLength(50)] string ModuleName,
    [Required, MaxLength(200)] string PageUrl,
    string? IconName,
    int SortOrder,
    bool IsMenu,
    bool IsBulk,
    bool IsVisible,
    bool IsActive,
    int? FormModuleId = null,
    string? FilterConfigJson = null
);

public class FormResponseDto
{
    public int FormId { get; set; }
    public int? FormModuleId { get; set; }
    public string FormCode { get; set; } = "";
    public string FormName { get; set; } = "";
    public string ModuleName { get; set; } = "";
    public string PageUrl { get; set; } = "";
    public bool IsBulk { get; set; }
    public bool IsActive { get; set; }
}
