using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using HRStackPK.Api.Models;
using Microsoft.IdentityModel.Tokens;

namespace HRStackPK.Api.Helpers;

public class JwtHelper
{
    private readonly string _key, _issuer, _audience;
    public readonly int AccessTokenMinutes;
    public readonly int RefreshTokenDays;

    public JwtHelper(IConfiguration cfg)
    {
        var j = cfg.GetSection("Jwt");
        _key      = j["Key"]!;
        _issuer   = j["Issuer"]!;
        _audience = j["Audience"]!;
        AccessTokenMinutes = int.TryParse(j["AccessTokenMinutes"], out var m) ? m : 60;
        RefreshTokenDays   = int.TryParse(j["RefreshTokenDays"],   out var d) ? d : 30;
    }

    /// <summary>Every API call downstream reads CompanyID + BranchID from these claims — never from the client.</summary>
    public string CreateAccessToken(AuthUser user, string loginType)
    {
        var claims = new List<Claim>
        {
            new("CompanyID",  user.TenantId.ToString()),
            new("BranchID",   user.BranchId.ToString()),
            new("EmployeeID", (user.EmployeeId ?? 0).ToString()),
            new("Role",       user.Role),
            new("RoleID",     user.RoleId.ToString()),
            new("LoginType",  loginType),                       // Head | Staff
            new(ClaimTypes.Name, user.FullName),
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var creds = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer:   _issuer,
            audience: _audience,
            claims:   claims,
            expires:  DateTime.UtcNow.AddMinutes(AccessTokenMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static string CreateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-").Replace("/", "_").TrimEnd('=');   // URL-safe
    }
}
