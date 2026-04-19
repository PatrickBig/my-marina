using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
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

    public static string PlatformOperatorToken()
        => GenerateToken(Guid.NewGuid(), "platform@mymarina.io", "PlatformAdmin");

    public static string MarinaOwnerToken(Guid tenantId, Guid? marinaId = null,
        SubscriptionTier tier = SubscriptionTier.Free)
        => GenerateToken(Guid.NewGuid(), "owner@marina.io", "TenantOwner", tenantId, marinaId, tier: tier);

    public static string MarinaStaffToken(Guid tenantId, Guid marinaId,
        SubscriptionTier tier = SubscriptionTier.Free)
        => GenerateToken(Guid.NewGuid(), "staff@marina.io", "MarinaStaff", tenantId, marinaId, tier: tier);

    public static string CustomerToken(Guid tenantId, Guid customerAccountId,
        SubscriptionTier tier = SubscriptionTier.Free)
        => GenerateToken(Guid.NewGuid(), "customer@portal.io", "Customer", tenantId,
            customerAccountId: customerAccountId, tier: tier);

    public static string DemoToken(Guid tenantId, string role,
        SubscriptionTier tier = SubscriptionTier.Pro)
        => GenerateToken(Guid.NewGuid(), $"demo-{role}@demo.mymarina.org", role, tenantId,
            tier: tier, isDemo: true);

    public static string GenerateToken(
        Guid userId,
        string email,
        string role,
        Guid? tenantId = null,
        Guid? marinaId = null,
        Guid? customerAccountId = null,
        IReadOnlyList<Guid>? customerAccountIds = null,
        SubscriptionTier tier = SubscriptionTier.Free,
        bool isDemo = false)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.GivenName, "Test"),
            new(JwtRegisteredClaimNames.FamilyName, "Customer"),
            new("role", role),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("subscription_tier", tier.ToString()),
        };

        if (isDemo)
            claims.Add(new Claim("is_demo", "true"));

        if (tenantId.HasValue)
            claims.Add(new Claim("tenant_id", tenantId.Value.ToString()));
        if (marinaId.HasValue)
            claims.Add(new Claim("marina_id", marinaId.Value.ToString()));

        // Emit both old (single account) and new (multiple accounts) claim formats for compatibility
        if (customerAccountIds?.Count > 0)
        {
            var idsJson = JsonSerializer.Serialize(customerAccountIds);
            claims.Add(new Claim("customer_account_ids", idsJson));
            // Also set the old claim format for backward compatibility
            claims.Add(new Claim("customer_account_id", customerAccountIds[0].ToString()));
        }
        else if (customerAccountId.HasValue)
        {
            claims.Add(new Claim("customer_account_id", customerAccountId.Value.ToString()));
            // Also emit the new format
            var idsJson = JsonSerializer.Serialize(new[] { customerAccountId.Value });
            claims.Add(new Claim("customer_account_ids", idsJson));
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

    /// <summary>Decodes a JWT and returns its claims as a flat string dictionary.</summary>
    public static Dictionary<string, string> ParseClaims(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);
        return jwt.Claims.ToDictionary(c => c.Type, c => c.Value);
    }
}
