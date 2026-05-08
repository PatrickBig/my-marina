using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MyMarina.Application.Abstractions;

namespace MyMarina.Infrastructure.Services;

public class JwtTokenService(IConfiguration configuration) : IJwtTokenService
{
    public string GenerateAccessToken(UserTokenInfo user, int? expiryMinutesOverride = null)
    {
        var key = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key is required.");
        var issuer = configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException("Jwt:Issuer is required.");
        var audience = configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException("Jwt:Audience is required.");
        var expiryMinutes = expiryMinutesOverride
            ?? (int.TryParse(configuration["Jwt:ExpiryMinutes"], out var m) ? m : 60);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("email_verified", user.EmailVerified.ToString().ToLowerInvariant()),
            new(JwtRegisteredClaimNames.GivenName, user.FirstName),
            new(JwtRegisteredClaimNames.FamilyName, user.LastName),
            new(JwtRegisteredClaimNames.Jti, Guid.CreateVersion7().ToString()),
        };

        if (user.IsPlatformOperator)
            claims.Add(new Claim("platform_role", "Operator"));

        if (user.IsDemo)
            claims.Add(new Claim("is_demo", "true"));

        if (user.Memberships.Count > 0)
        {
            var membershipsJson = JsonSerializer.Serialize(
                user.Memberships.Select(m => new
                {
                    scope = m.Scope.ToString(),
                    tenant_id = m.TenantId.ToString(),
                    marina_id = m.MarinaId?.ToString(),
                    role = m.Role.ToString(),
                    tier = m.Tier
                }));
            claims.Add(new Claim("memberships", membershipsJson));
        }

        if (user.BillingAccounts.Count > 0)
        {
            var billingJson = JsonSerializer.Serialize(
                user.BillingAccounts.Select(b => new
                {
                    billing_account_id = b.BillingAccountId.ToString(),
                    marina_id = b.MarinaId.ToString(),
                    role = b.Role.ToString()
                }));
            claims.Add(new Claim("billing_accounts", billingJson));
        }

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var expiry = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiry,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
