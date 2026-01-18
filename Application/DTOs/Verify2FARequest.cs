using System.ComponentModel.DataAnnotations;

public record Verify2FARequest(
    [Required]
    [EmailAddress]
    string Email,

    [Required(ErrorMessage = "Code is required")]
    string Code
);