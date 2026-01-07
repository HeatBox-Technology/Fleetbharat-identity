
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using FleetRobo.IdentityService.Domain.Entities;


public class AuthService : IAuthService
{
    private readonly IdentityDbContext _db;
    private readonly JwtTokenService _jwt;

    public AuthService(IdentityDbContext db, JwtTokenService jwt)
    {
        _db = db;
        _jwt = jwt;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest req)
    {
        var email = req.Email.Trim().ToLower();

        var user = await _db.Users
            .FirstOrDefaultAsync(x => x.Email.ToLower() == email);

        if (user == null)
            throw new UnauthorizedAccessException("Invalid email or password");

        if (!BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password");

        return await GenerateTokens(user);
    }

    public async Task<LoginResponse> RefreshAsync(RefreshTokenRequest req)
    {
        var user = await _db.Users.FirstAsync(x => x.RefreshToken == req.RefreshToken);
        if (user.RefreshTokenExpiry < DateTime.UtcNow)
            throw new UnauthorizedAccessException();

        return await GenerateTokens(user);
    }

    private async Task<LoginResponse> GenerateTokens(User user)
    {
        var access = _jwt.GenerateAccessToken(user);
        var refresh = _jwt.GenerateRefreshToken();

        user.RefreshToken = refresh;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        await _db.SaveChangesAsync();

        return new LoginResponse(access, refresh, DateTime.UtcNow.AddHours(1));
    }

    public async Task RegisterAsync(RegisterRequest req)
    {
        var email = req.Email.Trim().ToLower();

        var exists = await _db.Users.AnyAsync(x => x.Email == email);
        if (exists)
            throw new InvalidOperationException("Email already registered");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            Role = "User"
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();
    }

    public async Task ForgotPasswordAsync(string email)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == email);
        if (user == null) return; // security: no hint

        var token = Guid.NewGuid().ToString("N");

        user.PasswordResetTokenHash =
            BCrypt.Net.BCrypt.HashPassword(token);

        user.PasswordResetExpiry = DateTime.UtcNow.AddMinutes(15);
        await _db.SaveChangesAsync();

        // TODO: Send email
        // https://app/reset-password?token=token&email=email
    }

    public async Task ResetPasswordAsync(string email, string token, string newPassword)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == email);
        if (user == null ||
            user.PasswordResetExpiry < DateTime.UtcNow ||
            !BCrypt.Net.BCrypt.Verify(token, user.PasswordResetTokenHash))
            throw new UnauthorizedAccessException();

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.PasswordResetTokenHash = null;
        user.PasswordResetExpiry = null;

        await _db.SaveChangesAsync();
    }



}
