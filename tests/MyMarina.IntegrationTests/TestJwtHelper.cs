using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;
using MyMarina.Application.Abstractions;

namespace MyMarina.IntegrationTests;

public static class TestJwtHelper
{
    // Must match appsettings.json Jwt section
    public const string Key      = "CHANGE_ME_USE_ENV_VAR_IN_PRODUCTION_MIN_32_CHARS";
    public const string Issuer   = "mymarina-api";
    public const string Audience = "mymarina-clients";

    public static string PlatformOperatorToken(Guid? userId = null)
        => GenerateToken(userId ?? Guid.CreateVersion7(), "platform@mymarina.io",
            isPlatformOperator: true);

    public static string UserToken(
        Guid userId,
        string email,
        IReadOnlyList<MembershipClaim>? memberships = null,
        IReadOnlyList<BillingAccountMemberClaim>? billingAccounts = null,
        bool isDemo = false)
        => GenerateToken(userId, email,
            memberships: memberships,
            billingAccounts: billingAccounts,
            isDemo: isDemo);

    public static string GenerateToken(
        Guid userId,
        string email,
        bool isPlatformOperator = false,
        IReadOnlyList<MembershipClaim>? memberships = null,
        IReadOnlyList<BillingAccountMemberClaim>? billingAccounts = null,
        bool isDemo = false)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new("email_verified", "true"),
            new(JwtRegisteredClaimNames.GivenName, "Test"),
            new(JwtRegisteredClaimNames.FamilyName, "User"),
            new(JwtRegisteredClaimNames.Jti, Guid.CreateVersion7().ToString()),
        };

        if (isPlatformOperator)
            claims.Add(new Claim("platform_role", "Operator"));

        if (isDemo)
            claims.Add(new Claim("is_demo", "true"));

        if (memberships?.Count > 0)
        {
            var json = JsonSerializer.Serialize(memberships.Select(m => new
            {
                scope = m.Scope.ToString(),
                tenant_id = m.TenantId.ToString(),
                marina_id = m.MarinaId?.ToString(),
                role = m.Role.ToString(),
                tier = m.Tier
            }));
            claims.Add(new Claim("memberships", json));
        }

        if (billingAccounts?.Count > 0)
        {
            var json = JsonSerializer.Serialize(billingAccounts.Select(b => new
            {
                billing_account_id = b.BillingAccountId.ToString(),
                marina_id = b.MarinaId.ToString(),
                role = b.Role.ToString()
            }));
            claims.Add(new Claim("billing_accounts", json));
        }

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key));
        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
