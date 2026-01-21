using System.ComponentModel.DataAnnotations;

public record LoginRequest(
    [Required, EmailAddress]
    string Email,
    string Password
);