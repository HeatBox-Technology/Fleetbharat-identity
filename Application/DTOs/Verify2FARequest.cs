using System;
using System.ComponentModel.DataAnnotations;

public class Verify2FARequest
{
    [Required]
    public Guid UserId { get; set; }

    [Required(ErrorMessage = "Code is required")]
    public string Code { get; set; } = "";
}
