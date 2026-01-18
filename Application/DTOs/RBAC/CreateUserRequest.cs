using System.ComponentModel.DataAnnotations;

public class CreateUserRequest
{
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required] public string Password { get; set; } = "";

    [Required] public string FirstName { get; set; } = "";
    [Required] public string LastName { get; set; } = "";

    [Required] public int AccountId { get; set; }
    [Required] public int RoleId { get; set; }
}
