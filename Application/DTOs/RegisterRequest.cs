using System.ComponentModel.DataAnnotations;

public record RegisterRequest(
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Email is not valid")]
    string Email,

    [Required(ErrorMessage = "Password is required")]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
    string Password,

    [Required(ErrorMessage = "FirstName is required")]
    [MinLength(2, ErrorMessage = "FirstName must be at least 2 characters")]
    [MaxLength(50, ErrorMessage = "FirstName can be max 50 characters")]
    string FirstName,

    [Required(ErrorMessage = "LastName is required")]
    [MinLength(2, ErrorMessage = "LastName must be at least 2 characters")]
    [MaxLength(50, ErrorMessage = "LastName can be max 50 characters")]
    string LastName,

    [Required(ErrorMessage = "MobileNo is required")]
    [RegularExpression(@"^\d{10}$", ErrorMessage = "MobileNo must be exactly 10 digits")]
    string MobileNo,

    [Required(ErrorMessage = "CountryCode is required")]
    [RegularExpression(@"^\+\d{1,3}$", ErrorMessage = "CountryCode must be like +91, +1, +44")]
    string CountryCode,

    [MaxLength(20, ErrorMessage = "RefferalCode can be max 20 characters")]
    string? RefferalCode
);
