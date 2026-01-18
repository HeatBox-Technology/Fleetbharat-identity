using System.ComponentModel.DataAnnotations;

public record ResetPasswordRequest(
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Email is not valid")]
    string Email,

    [Required(ErrorMessage = "Token is required")]
    string Token,

    [Required(ErrorMessage = "NewPassword is required")]
    [MinLength(8, ErrorMessage = "NewPassword must be at least 8 characters")]
    [RegularExpression(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{8,}$",
        ErrorMessage = "Password must contain uppercase, lowercase, number and special character"
    )]
    string NewPassword,

    [Required(ErrorMessage = "ConfirmPassword is required")]
    string ConfirmPassword
);
