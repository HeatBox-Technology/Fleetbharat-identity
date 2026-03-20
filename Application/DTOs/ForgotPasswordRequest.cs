using System.ComponentModel.DataAnnotations;

public record ForgotPasswordRequest(
    [Required]
    [EmailAddress]
    string Email
);