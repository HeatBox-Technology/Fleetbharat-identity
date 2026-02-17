
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;


public class AuthService : IAuthService
{
    private readonly IdentityDbContext _db;
    private readonly JwtTokenService _jwt;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _config;
    private readonly int _accessTokenExpiryMinutes;

    public AuthService(IdentityDbContext db, JwtTokenService jwt, IEmailService emailService, IConfiguration config)
    {
        _db = db;
        _jwt = jwt;
        _emailService = emailService;
        _config = config;
        _accessTokenExpiryMinutes = Math.Max(1, _config.GetValue<int?>("Jwt:AccessTokenExpiryMinutes") ?? 60);
    }

    public async Task<LoginWithAccessResponse> LoginAsync(LoginRequest req)
    {
        var loginInput = req.Email?.Trim();
        if (string.IsNullOrWhiteSpace(loginInput))
            throw new UnauthorizedAccessException("Invalid email or password");
        var normalizedLoginInput = loginInput.ToLower();

        var user = await _db.Users
            .FirstOrDefaultAsync(x =>
                !x.IsDeleted &&
                (
                    x.Email.ToLower() == normalizedLoginInput ||
                    x.MobileNo.ToLower() == normalizedLoginInput ||
                    (x.User_name != null && x.User_name.ToLower() == normalizedLoginInput)
                ));

        if (user == null)
            throw new UnauthorizedAccessException("Invalid email or password");

        if (!BCrypt.Net.BCrypt.Verify(req.Password, user.Password_hash))
            throw new UnauthorizedAccessException("Invalid email or password");

        // -------------------------------------------------
        // ✅ 2FA handling
        // -------------------------------------------------
        if (user.TwoFactorEnabled)
        {
            var code = new Random().Next(100000, 999999).ToString();

            user.TwoFactorCodeHash = BCrypt.Net.BCrypt.HashPassword(code);
            user.TwoFactorExpiry = DateTime.UtcNow.AddMinutes(5);
            await _db.SaveChangesAsync();

            await _emailService.SendAsync(
                user.Email,
                "Your Login Verification Code",
                $"<h2>{code}</h2><p>Expires in 5 minutes</p>"
            );

            return new LoginWithAccessResponse
            {
                UserId = user.UserId,
                Email = user.Email,
                FullName = $"{user.FirstName} {user.LastName}".Trim(),
                ProfileImagePath = user.ProfileImagePath ?? "",

                Token = new LoginResponse(
                    AccessToken: null,
                    RefreshToken: null,
                    ExpiresAt: null,
                    Is2FARequired: true,
                    Message: "2FA code sent to your email"
                ),

                FormRights = new List<FormRightResponseDto>(),
                WhiteLabel = new WhiteLabelInfoDto()
            };
        }

        // -------------------------------------------------
        // ✅ Generate tokens
        // -------------------------------------------------
        var tokens = await GenerateTokens(user);

        // -------------------------------------------------
        // ✅ Fetch role, account, white-label
        // -------------------------------------------------
        var role = await _db.Roles
            .FirstOrDefaultAsync(r => r.RoleId == user.roleId);

        var account = await _db.Accounts
            .FirstOrDefaultAsync(a => a.AccountId == user.AccountId && !a.IsDeleted);

        var whiteLabel = await _db.WhiteLabels
            .AsNoTracking()
            .FirstOrDefaultAsync(w =>
                w.AccountId == user.AccountId &&
                w.IsActive);

        // -------------------------------------------------
        // ✅ Fetch role-based form rights
        // -------------------------------------------------
        var rights = await (
            from rr in _db.FormRoleRights
            join f in _db.Forms on rr.FormId equals f.FormId
            where rr.RoleId == user.roleId
            select new FormRightResponseDto
            {
                FormId = f.FormId,
                FormCode = f.FormCode,
                FormName = f.FormName,
                CanRead = rr.CanRead,
                CanWrite = rr.CanWrite,
                CanDelete = rr.CanDelete,
                CanExport = rr.CanExport,
                CanAll = rr.CanAll
            }
        ).ToListAsync();

        // -------------------------------------------------
        // ✅ Final response
        // -------------------------------------------------
        return new LoginWithAccessResponse
        {
            UserId = user.UserId,
            Email = user.Email,
            FullName = $"{user.FirstName} {user.LastName}".Trim(),
            ProfileImagePath = user.ProfileImagePath ?? "",

            AccountId = user.AccountId,
            AccountName = account?.AccountName ?? "",
            AccountCode = account?.AccountCode ?? "",

            RoleId = user.roleId,
            RoleName = role?.RoleName ?? "",

            Token = new LoginResponse(
                AccessToken: tokens.AccessToken,
                RefreshToken: tokens.RefreshToken,
                ExpiresAt: tokens.ExpiresAt,
                Is2FARequired: false,
                Message: "Login successful"
            ),

            WhiteLabel = whiteLabel == null
                ? new WhiteLabelInfoDto()
                : new WhiteLabelInfoDto
                {
                    WhiteLabelId = whiteLabel.WhiteLabelId,
                    CustomEntryFqdn = whiteLabel.CustomEntryFqdn,
                    LogoUrl = whiteLabel.LogoUrl,
                    PrimaryColorHex = whiteLabel.PrimaryColorHex,
                    SecondaryColorHex = whiteLabel.SecondaryColorHex
                },

            FormRights = rights
        };
    }



    public async Task<LoginResponse> RefreshAsync(RefreshTokenRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.RefreshToken))
            throw new UnauthorizedAccessException("Invalid refresh token");

        var refreshToken = req.RefreshToken.Trim();
        var user = await _db.Users.FirstOrDefaultAsync(x => x.RefreshToken == refreshToken && !x.IsDeleted);

        if (user == null)
            throw new UnauthorizedAccessException("Invalid refresh token");

        if (user.RefreshTokenExpiry <= DateTime.UtcNow)
            throw new UnauthorizedAccessException("Refresh token expired");

        return await GenerateTokens(user);
    }

    private async Task<LoginResponse> GenerateTokens(User user)
    {
        var access = _jwt.GenerateAccessToken(user);
        var refresh = _jwt.GenerateRefreshToken();

        user.RefreshToken = refresh;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        await _db.SaveChangesAsync();

        return new LoginResponse(access, refresh, DateTime.UtcNow.AddMinutes(_accessTokenExpiryMinutes));
    }

    public async Task RegisterAsync(RegisterRequest req)
    {
        var email = req.Email.Trim().ToLower();

        var exists = await _db.Users.AnyAsync(x => x.Email == email);
        if (exists)
            throw new BadHttpRequestException("Email already registered"); // ✅ 400 type error

        var user = new User
        {
            Email = email,
            FirstName = req.FirstName,
            LastName = req.LastName,
            MobileNo = req.MobileNo,
            CountryCode = req.CountryCode,
            Password_hash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            ReferralCode = req.RefferalCode,
            CreatedAt = DateTime.UtcNow,
            Status = true,
            roleId = 0 // Default role, e.g., 'User'
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();
    }


    public async Task ForgotPasswordAsync(string email)
    {
        email = email.Trim().ToLower();

        var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == email);
        if (user == null) return; // ✅ security: no hint

        var token = Guid.NewGuid().ToString("N");

        user.PasswordResetTokenHash = BCrypt.Net.BCrypt.HashPassword(token);
        user.PasswordResetExpiry = DateTime.UtcNow.AddMinutes(15);

        await _db.SaveChangesAsync();

        var resetBaseUrl = _config["Frontend:ResetPasswordUrl"]!;
        var resetLink = $"{resetBaseUrl}?token={token}&email={email}";

        var subject = "Reset your password";
        var htmlBody = $@"
        <h3>Password Reset Request</h3>
        <p>Hello {user.FirstName},</p>
        <p>You requested to reset your password. Click below:</p>
        <p><a href='{resetLink}'>Reset Password</a></p>
        <p>This link will expire in 15 minutes.</p>
        <br/>
        <p>If you did not request this, ignore this email.</p>
    ";

        await _emailService.SendAsync(email, subject, htmlBody);
    }


    public async Task<LoginResponse> ResetPasswordAsync(ResetPasswordRequest req)
    {
        if (req.NewPassword != req.ConfirmPassword)
            throw new BadHttpRequestException("NewPassword and ConfirmPassword do not match");

        var email = req.Email.Trim().ToLower();

        var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == email);

        if (user == null)
            throw new UnauthorizedAccessException("Invalid token or expired");

        if (string.IsNullOrWhiteSpace(user.PasswordResetTokenHash))
            throw new UnauthorizedAccessException("Invalid token or expired");

        if (!user.PasswordResetExpiry.HasValue || user.PasswordResetExpiry.Value < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Token expired");

        var isValidToken = BCrypt.Net.BCrypt.Verify(req.Token, user.PasswordResetTokenHash);

        if (!isValidToken)
            throw new UnauthorizedAccessException("Invalid token or expired");

        user.Password_hash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);

        user.PasswordResetTokenHash = null;
        user.PasswordResetExpiry = null;

        user.UpdatedAt = DateTime.UtcNow; // remove if you don't have UpdatedAt

        await _db.SaveChangesAsync();

        // ✅ Auto login: generate JWT token response
        return await GenerateTokens(user);
    }
    public async Task<LoginResponse> Verify2FAAsync(Verify2FARequest req)
    {
        var email = req.Email.Trim().ToLower();

        var user = await _db.Users.FirstOrDefaultAsync(x => x.Email.ToLower() == email);

        if (user == null)
            throw new UnauthorizedAccessException("Invalid request");

        if (!user.TwoFactorEnabled)
            throw new BadHttpRequestException("2FA is not enabled for this user");

        if (string.IsNullOrWhiteSpace(user.TwoFactorCodeHash))
            throw new UnauthorizedAccessException("2FA code not found or expired");

        if (!user.TwoFactorExpiry.HasValue || user.TwoFactorExpiry.Value < DateTime.UtcNow)
            throw new UnauthorizedAccessException("2FA code expired");

        var isValid = BCrypt.Net.BCrypt.Verify(req.Code, user.TwoFactorCodeHash);

        if (!isValid)
            throw new UnauthorizedAccessException("Invalid 2FA code");
        //Clear 2FA code after successful verification
        user.TwoFactorCodeHash = null;
        user.TwoFactorExpiry = null;

        await _db.SaveChangesAsync();

        // ✅ Generate tokens now
        var tokens = await GenerateTokens(user);


        return new LoginResponse
        (
            Is2FARequired: false,
            Message: "Login successful",
            AccessToken: tokens.AccessToken,
            RefreshToken: tokens.RefreshToken,
            ExpiresAt: tokens.ExpiresAt
        );
    }

}

