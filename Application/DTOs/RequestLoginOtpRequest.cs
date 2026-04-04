using System.ComponentModel.DataAnnotations;

public class RequestLoginOtpRequest
{
    [Required(ErrorMessage = "Phone is required")]
    public string Phone { get; set; } = "";
}
