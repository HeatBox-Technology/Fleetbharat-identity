using System;
using System.Collections.Generic;

public class LoginWithAccessResponse
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = "";
    public string FullName { get; set; } = "";
    public string ProfileImagePath { get; set; } = "";

    public int AccountId { get; set; }
    public string AccountName { get; set; } = "";
    public string AccountCode { get; set; } = "";


    public int RoleId { get; set; }
    public string RoleName { get; set; } = "";

    public LoginResponse Token { get; set; } = new(null, null, null);
    public WhiteLabelInfoDto WhiteLabel { get; set; } = new();
    public List<FormRightResponseDto> FormRights { get; set; } = new();
}
public class WhiteLabelInfoDto
{
    public int WhiteLabelId { get; set; }
    public string CustomEntryFqdn { get; set; } = "";
    public string? LogoUrl { get; set; }
    public string PrimaryColorHex { get; set; } = "";
    public string? SecondaryColorHex { get; set; } = null;
}