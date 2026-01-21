using System;

public record LoginResponse(
string? AccessToken, string? RefreshToken, DateTime? ExpiresAt,
bool Is2FARequired = false, string Message = ""
);

public class FormRightResponseDto
{
    public int FormId { get; set; }
    public string FormCode { get; set; } = "";
    public string FormName { get; set; } = "";

    public bool CanRead { get; set; }
    public bool CanWrite { get; set; }
    public bool CanUpdate { get; set; }
    public bool CanDelete { get; set; }
    public bool CanExport { get; set; }
    public bool CanAll { get; set; }
}