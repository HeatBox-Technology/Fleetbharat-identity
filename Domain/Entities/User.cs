
using System;

namespace FleetRobo.IdentityService.Domain.Entities;

public class User
{
    public Guid UserId { get; set; }                 // UUID
    public int AccountId { get; set; }              // FK to Account
    public int roleId { get; set; }                 // FK to Role
    public string Role { get; set; } = "User";
    public string? GlobalUserCode { get; set; }       // FR-USR-IND-0001

    public string FirstName { get; set; }

    public string LastName { get; set; } = "";

    public string Email { get; set; } = "";

    public string MobileNo { get; set; } = "0000000000";        // With country code

    public string CountryCode { get; set; } = "";         // ISO-3 (IND, USA)

    public string Timezone { get; set; } = "";             // Asia/Kolkata

    public string Language { get; set; } = "";            // en, ar, fr
    public string Password_hash { get; set; } = "";

    public bool Status { get; set; }                 // Active / Inactive

    public int UserStatusLkpId { get; set; }          // Lookup status

    public bool EmailVerified { get; set; }

    public bool MobileVerified { get; set; }

    public bool TwoFactorEnabled { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public string? ReferralCode { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime RefreshTokenExpiry { get; set; }
    public string? PasswordResetTokenHash { get; set; }
    public DateTime? PasswordResetExpiry { get; set; }

}
