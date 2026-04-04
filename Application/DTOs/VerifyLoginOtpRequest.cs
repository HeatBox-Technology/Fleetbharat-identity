using System;
using System.ComponentModel.DataAnnotations;

public class VerifyLoginOtpRequest
{
    [Required]
    public Guid UserId { get; set; }

    [Required(ErrorMessage = "OTP is required")]
    public string Otp { get; set; } = "";
}
