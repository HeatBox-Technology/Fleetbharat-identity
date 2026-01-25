using System;
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
public class UserCardSummaryDto
{
    public int TotalUsers { get; set; }
    public int Active { get; set; }
    public int SuspendedOrLocked { get; set; }
    public int TwoFactorEnabled { get; set; }
}
public class UserListItemDto
{
    public Guid UserId { get; set; }

    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";

    public int RoleId { get; set; }
    public string RoleName { get; set; } = "";

    public int AccountId { get; set; }
    public string AccountName { get; set; } = "";

    public bool Status { get; set; }              // Active / Inactive

    public bool TwoFactorEnabled { get; set; }    // 2FA badge
    public DateTime? LastLoginAt { get; set; }    // last login
}
public class UserListUiResponseDto
{
    public UserCardSummaryDto Summary { get; set; } = new();
    public PagedResultDto<UserListItemDto> Users { get; set; } = new();
}
public class UserDetailResponseDto
{
    public Guid UserId { get; set; }

    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";

    public string Email { get; set; } = "";
    public string MobileNo { get; set; } = "";
    public string CountryCode { get; set; } = "";

    public int AccountId { get; set; }
    public int RoleId { get; set; }

    public bool Status { get; set; }
    public bool TwoFactorEnabled { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
public class UpdateUserRequest
{
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";

    public int AccountId { get; set; }
    public int RoleId { get; set; }

    public string MobileNo { get; set; } = "";
    public string CountryCode { get; set; } = "";

    public bool Status { get; set; }
    public bool TwoFactorEnabled { get; set; }
}


