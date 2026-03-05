
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

public class JwtTokenService
{
    private readonly IConfiguration _config;
    private readonly int _accessTokenExpiryMinutes;
    public JwtTokenService(IConfiguration config)
    {
        _config = config;
        _accessTokenExpiryMinutes = Math.Max(1, _config.GetValue<int?>("Jwt:AccessTokenExpiryMinutes") ?? 60);
    }

    public string GenerateAccessToken(
        User user,
        string roleName,
        string hierarchyPath,
        bool isSystemRole,
        IReadOnlyCollection<int>? accessibleAccountIds)
    {
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new Claim("UserId", user.UserId.ToString()),
            new Claim("AccountId", user.AccountId.ToString()),
            new Claim("accountId", user.AccountId.ToString()),
            new Claim("RoleId", user.roleId.ToString()),
            new Claim("roleId", user.roleId.ToString()),
            new Claim("IsSystemRole", isSystemRole.ToString().ToLowerInvariant()),
            new Claim("HierarchyPath", hierarchyPath ?? string.Empty),
            new Claim(ClaimTypes.Role, roleName ?? string.Empty)
        };

        if (!isSystemRole && accessibleAccountIds != null && accessibleAccountIds.Count > 0)
        {
            claims.Add(new Claim(
                "AccessibleAccountIds",
                string.Join(",", accessibleAccountIds.Distinct().OrderBy(x => x))));
        }

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));

        var token = new JwtSecurityToken(
            _config["Jwt:Issuer"],
            _config["Jwt:Audience"],
            claims,
            expires: DateTime.UtcNow.AddMinutes(_accessTokenExpiryMinutes),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
        => Convert.ToBase64String(Guid.NewGuid().ToByteArray());
}
