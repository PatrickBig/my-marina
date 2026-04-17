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
    public string GenerateToken(UserTokenInfo user)
    {
        var key = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key is required.");
        var issuer = configuration["Jwt:Issuer"]!;
        var audience = configuration["Jwt:Audience"]!;
        var expiryMinutes = int.TryParse(configuration["Jwt:ExpiryMinutes"], out var m) ? m : 60;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.GivenName, user.FirstName),
            new(JwtRegisteredClaimNames.FamilyName, user.LastName),
            new(ClaimTypes.Role, user.Role ?? "Unknown"),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("has_multiple_contexts", user.HasMultipleContexts.ToString().ToLower()),
        };

        if (user.TenantId.HasValue)
            claims.Add(new Claim("tenant_id", user.TenantId.Value.ToString()));

        if (user.MarinaId.HasValue)
            claims.Add(new Claim("marina_id", user.MarinaId.Value.ToString()));

        if (user.CustomerAccountIds?.Count > 0)
        {
            var idsJson = JsonSerializer.Serialize(user.CustomerAccountIds);
            claims.Add(new Claim("customer_account_ids", idsJson));
            // Always emit the singular claim for backward compatibility with portal queries
            claims.Add(new Claim("customer_account_id", user.CustomerAccountIds[0].ToString()));
        }
        else if (user.CustomerAccountId.HasValue)
        {
            // Backward compatibility: if only single CustomerAccountId is set, emit both old and new claim formats
            claims.Add(new Claim("customer_account_id", user.CustomerAccountId.Value.ToString()));
            var idsJson = JsonSerializer.Serialize(new[] { user.CustomerAccountId.Value });
            claims.Add(new Claim("customer_account_ids", idsJson));
        }

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
