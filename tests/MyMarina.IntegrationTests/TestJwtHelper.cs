using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using MyMarina.Domain.Enums;

namespace MyMarina.IntegrationTests;

/// <summary>
/// Generates signed JWTs for use in integration tests without going through the login endpoint.
/// Uses the same key/issuer/audience configured in appsettings.json so the API accepts them.
/// </summary>
public static class TestJwtHelper
{
    // Must match appsettings.json Jwt section
    public const string Key      = "CHANGE_ME_USE_ENV_VAR_IN_PRODUCTION_MIN_32_CHARS";
    public const string Issuer   = "mymarina-api";
    public const string Audience = "mymarina-clients";

    public static string GenerateToken(
        Guid userId,
        string email,
        UserRole role,
        Guid? tenantId = null,
        Guid? marinaId = null)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.GivenName, "Test"),
            new(JwtRegisteredClaimNames.FamilyName, "User"),
            new(ClaimTypes.Role, role.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        if (tenantId.HasValue)
            claims.Add(new Claim("tenant_id", tenantId.Value.ToString()));

        if (marinaId.HasValue)
            claims.Add(new Claim("marina_id", marinaId.Value.ToString()));

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key));
        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static string PlatformOperatorToken()
        => GenerateToken(Guid.NewGuid(), "platform@mymarina.io", UserRole.PlatformOperator);

    public static string MarinaOwnerToken(Guid tenantId, Guid? marinaId = null)
        => GenerateToken(Guid.NewGuid(), "owner@marina.io", UserRole.MarinaOwner, tenantId, marinaId);

    public static string MarinaStaffToken(Guid tenantId, Guid marinaId)
        => GenerateToken(Guid.NewGuid(), "staff@marina.io", UserRole.MarinaStaff, tenantId, marinaId);
}
