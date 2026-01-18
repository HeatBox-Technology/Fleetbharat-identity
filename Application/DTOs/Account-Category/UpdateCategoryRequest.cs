using System.ComponentModel.DataAnnotations;

public record UpdateCategoryRequest(
    [Required(ErrorMessage = "LabelName is required")]
    [MaxLength(100)]
    string LabelName,

    [MaxLength(500)]
    string? Description,

    bool IsActive
);
