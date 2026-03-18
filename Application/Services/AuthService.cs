
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;


public class AuthService : IAuthService
{
    private const int MaxTwoFactorAttempts = 5;
    private static readonly ConcurrentDictionary<Guid, int> TwoFactorAttemptTracker = new();

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
                    (x.Email != null && x.Email.ToLower() == normalizedLoginInput) ||
                    (x.User_name != null && x.User_name.ToLower() == normalizedLoginInput) ||
                    (x.MobileNo != null && x.MobileNo == loginInput)
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
            var code = RandomNumberGenerator
                .GetInt32(100000, 1000000)
                .ToString();

            TwoFactorAttemptTracker.TryRemove(user.UserId, out _);

            user.TwoFactorCodeHash = BCrypt.Net.BCrypt.HashPassword(code);
            user.TwoFactorExpiry = DateTime.UtcNow.AddMinutes(5);
            await _db.SaveChangesAsync();

            var template = await LoadEmailTemplateAsync("fleetbharat-2fa.html");
            var body = template.Replace("{{CODE}}", code);

            await _emailService.SendAsync(
                user.Email,
                "Fleetbharat Login Verification Code",
                body
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
                    Message: "2FA verification required"
                ),

                FormRights = new List<FormRightResponseDto>(),
                WhiteLabel = new WhiteLabelInfoDto()
            };
        }

        // -------------------------------------------------
        // ✅ Fetch role, account, white-label
        // -------------------------------------------------
        var role = await _db.Roles
            .FirstOrDefaultAsync(r => r.RoleId == user.roleId);

        var account = await _db.Accounts
            .FirstOrDefaultAsync(a => a.AccountId == user.AccountId && !a.IsDeleted);

        var roleName = ResolveRoleName(role, user);
        var isSystemRole = ResolveIsSystemRole(role, roleName);

        // -------------------------------------------------
        // ✅ Generate tokens
        // -------------------------------------------------
        var tokens = await GenerateTokens(
            user,
            roleName,
            account?.HierarchyPath ?? string.Empty,
            isSystemRole);

        var whiteLabel = await _db.WhiteLabels
            .AsNoTracking()
            .FirstOrDefaultAsync(w =>
                w.AccountId == user.AccountId &&
                w.IsActive);

        // -------------------------------------------------
        // ✅ Fetch role-based form rights
        // -------------------------------------------------
        var rights = await GetRoleFormRightsAsync(user.roleId, roleName);

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
            RoleName = roleName,

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
                    BrandName = whiteLabel.BrandName,
                    LogoUrl = whiteLabel.LogoUrl,
                    LogoName = whiteLabel.LogoName,
                    LogoPath = whiteLabel.LogoPath,
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

        var role = await _db.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.RoleId == user.roleId);

        var account = await _db.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.AccountId == user.AccountId && !a.IsDeleted);

        var roleName = ResolveRoleName(role, user);

        return await GenerateTokens(
            user,
            roleName,
            account?.HierarchyPath ?? string.Empty,
            ResolveIsSystemRole(role, roleName));
    }

    private async Task<LoginResponse> GenerateTokens(
        User user,
        string roleName,
        string hierarchyPath,
        bool isSystemRole)
    {
        var accessibleAccountIds = await BuildAccessibleAccountIdsAsync(
            hierarchyPath,
            user.AccountId,
            isSystemRole);

        var access = _jwt.GenerateAccessToken(
            user,
            roleName,
            hierarchyPath,
            isSystemRole,
            accessibleAccountIds);
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

        var template = await LoadEmailTemplateAsync("fleetbharat-reset-password.html");
        var body = template
            .Replace("{{FIRSTNAME}}", user.FirstName ?? "")
            .Replace("{{RESET_LINK}}", resetLink);

        await _emailService.SendAsync(
            email,
            "Fleetbharat Password Reset Request",
            body
        );
    }


    public async Task<LoginResponse> ResetPasswordAsync(ResetPasswordRequest req)
    {
        if (req.NewPassword != req.ConfirmPassword)
            throw new BadHttpRequestException("NewPassword and ConfirmPassword do not match");

        if (string.IsNullOrWhiteSpace(req.Email))
            throw new BadHttpRequestException("Email is required");
        if (string.IsNullOrWhiteSpace(req.Token))
            throw new BadHttpRequestException("Token is required");

        var email = req.Email.Trim().ToLowerInvariant();
        var token = req.Token.Trim();

        var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == email && !x.IsDeleted);

        if (user == null)
            throw new UnauthorizedAccessException("Invalid token or expired");

        if (string.IsNullOrWhiteSpace(user.PasswordResetTokenHash))
            throw new UnauthorizedAccessException("Invalid token or expired");

        if (!user.PasswordResetExpiry.HasValue || user.PasswordResetExpiry.Value < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Token expired");

        var isValidToken = BCrypt.Net.BCrypt.Verify(token, user.PasswordResetTokenHash);

        if (!isValidToken)
            throw new UnauthorizedAccessException("Invalid token or expired");

        user.Password_hash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);

        user.PasswordResetTokenHash = null;
        user.PasswordResetExpiry = null;

        user.UpdatedAt = DateTime.UtcNow; // remove if you don't have UpdatedAt

        await _db.SaveChangesAsync();

        // ✅ Auto login: generate JWT token response
        var role = await _db.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.RoleId == user.roleId);

        var account = await _db.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.AccountId == user.AccountId && !a.IsDeleted);

        var roleName = ResolveRoleName(role, user);

        return await GenerateTokens(
            user,
            roleName,
            account?.HierarchyPath ?? string.Empty,
            ResolveIsSystemRole(role, roleName));
    }
    public async Task<LoginWithAccessResponse> Verify2FAAsync(Verify2FARequest req)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(x => x.UserId == req.UserId && !x.IsDeleted);

        if (user == null)
            throw new UnauthorizedAccessException("Invalid request");

        if (!user.TwoFactorEnabled)
            throw new BadHttpRequestException("2FA is not enabled for this user");

        if (string.IsNullOrWhiteSpace(user.TwoFactorCodeHash))
            throw new UnauthorizedAccessException("2FA code not found or expired");

        if (!user.TwoFactorExpiry.HasValue)
            throw new UnauthorizedAccessException("2FA code expired");

        if (user.TwoFactorExpiry.Value < DateTime.UtcNow)
        {
            TwoFactorAttemptTracker.TryRemove(user.UserId, out _);
            throw new UnauthorizedAccessException("Verification code expired");
        }

        var isValid = BCrypt.Net.BCrypt.Verify(req.Code, user.TwoFactorCodeHash);

        if (!isValid)
        {
            var attempts = TwoFactorAttemptTracker.AddOrUpdate(user.UserId, 1, (_, current) => current + 1);
            if (attempts >= MaxTwoFactorAttempts)
            {
                user.TwoFactorCodeHash = null;
                user.TwoFactorExpiry = null;
                await _db.SaveChangesAsync();
                TwoFactorAttemptTracker.TryRemove(user.UserId, out _);
                throw new UnauthorizedAccessException("Too many invalid attempts. Request a new verification code.");
            }

            throw new UnauthorizedAccessException("Invalid verification code");
        }
        //Clear 2FA code after successful verification
        user.TwoFactorCodeHash = null;
        user.TwoFactorExpiry = null;
        TwoFactorAttemptTracker.TryRemove(user.UserId, out _);

        await _db.SaveChangesAsync();

        // ✅ Generate tokens now
        var role = await _db.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.RoleId == user.roleId);

        var account = await _db.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.AccountId == user.AccountId && !a.IsDeleted);

        var roleName = ResolveRoleName(role, user);

        var tokens = await GenerateTokens(
            user,
            roleName,
            account?.HierarchyPath ?? string.Empty,
            ResolveIsSystemRole(role, roleName));


        var whiteLabel = await _db.WhiteLabels
            .AsNoTracking()
            .FirstOrDefaultAsync(w =>
                w.AccountId == user.AccountId &&
                w.IsActive);

        var rights = await GetRoleFormRightsAsync(user.roleId, roleName);

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
            RoleName = roleName,

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
                    BrandName = whiteLabel.BrandName,
                    LogoUrl = whiteLabel.LogoUrl,
                    LogoName = whiteLabel.LogoName,
                    LogoPath = whiteLabel.LogoPath,
                    PrimaryColorHex = whiteLabel.PrimaryColorHex,
                    SecondaryColorHex = whiteLabel.SecondaryColorHex
                },

            FormRights = rights
        };
    }

    private static string ResolveRoleName(mst_role? role, User user)
    {
        if (role?.IsSystemRole == true)
            return "System";

        if (!string.IsNullOrWhiteSpace(role?.RoleName))
            return role.RoleName;

        return user.Role ?? string.Empty;
    }

    private static bool ResolveIsSystemRole(mst_role? role, string roleName) =>
        role?.IsSystemRole == true ||
        roleName.Equals("System", StringComparison.OrdinalIgnoreCase);

    private async Task<IReadOnlyCollection<int>> BuildAccessibleAccountIdsAsync(
        string hierarchyPath,
        int accountId,
        bool isSystemRole)
    {
        if (isSystemRole)
            return Array.Empty<int>();

        if (string.IsNullOrWhiteSpace(hierarchyPath))
            return new[] { accountId };

        var accountIds = await _db.Accounts
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.HierarchyPath.StartsWith(hierarchyPath))
            .Select(x => x.AccountId)
            .ToListAsync();

        if (accountIds.Count == 0)
            accountIds.Add(accountId);

        return accountIds;
    }

    private async Task<List<FormRightResponseDto>> GetRoleFormRightsAsync(int roleId, string roleName)
    {
        if (roleName.Equals("System", StringComparison.OrdinalIgnoreCase))
        {
            return await _db.Forms
                .AsNoTracking()
                .OrderBy(f => f.SortOrder)
                .Select(f => new FormRightResponseDto
                {
                    FormId = f.FormId,
                    FormCode = f.FormCode,
                    FormName = f.FormName,
                    IsBulk = f.IsBulk,
                    PageUrl = f.PageUrl,
                    icon = f.IconName,
                    CanRead = true,
                    CanWrite = true,
                    CanUpdate = true,
                    CanDelete = true,
                    CanExport = true,
                    CanAll = true
                })
                .ToListAsync();
        }

        return await (
            from rr in _db.FormRoleRights
            join f in _db.Forms on rr.FormId equals f.FormId
            where rr.RoleId == roleId
            select new FormRightResponseDto
            {
                FormId = f.FormId,
                FormCode = f.FormCode,
                FormName = f.FormName,
                IsBulk = f.IsBulk,
                PageUrl = f.PageUrl,
                icon = f.IconName,
                CanRead = rr.CanRead,
                CanWrite = rr.CanWrite,
                CanUpdate = rr.CanUpdate,
                CanDelete = rr.CanDelete,
                CanExport = rr.CanExport,
                CanAll = rr.CanAll
            }
        ).ToListAsync();
    }

    private static async Task<string> LoadEmailTemplateAsync(string templateName)
    {
        var relativePath = Path.Combine("docs", "email-templates", templateName);
        var contentRootPath = Path.Combine(Directory.GetCurrentDirectory(), relativePath);
        if (File.Exists(contentRootPath))
            return await File.ReadAllTextAsync(contentRootPath);

        var baseDirectoryPath = Path.Combine(AppContext.BaseDirectory, relativePath);
        if (File.Exists(baseDirectoryPath))
            return await File.ReadAllTextAsync(baseDirectoryPath);

        throw new FileNotFoundException($"Email template not found: {relativePath}");
    }

}



