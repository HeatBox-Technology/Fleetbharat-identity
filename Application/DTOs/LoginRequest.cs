using System.ComponentModel.DataAnnotations;

public record LoginRequest(
    [Required]
    string Email,
    string Password
);
