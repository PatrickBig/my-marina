using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Identity;
using MyMarina.Domain.Entities;

namespace MyMarina.Infrastructure.Identity;

/// <summary>
/// Shared logic reused by login and external-auth handlers.
/// </summary>
internal static class AuthHelpers
{
    internal static async Task<UserTokenInfo> BuildTokenInfoAsync(
        UserManager<ApplicationUser> userManager,
        ApplicationUser user)
    {
        var isPlatformOp = await userManager.IsInRoleAsync(user, "PlatformOperator");
        return new UserTokenInfo(
            UserId: user.Id,
            Email: user.Email!,
            EmailVerified: user.EmailConfirmed,
            FirstName: user.FirstName,
            LastName: user.LastName,
            IsPlatformOperator: isPlatformOp,
            IsDemo: false,
            Memberships: [],
            BillingAccounts: []
        );
    }

    internal static (string raw, string hash, DateTimeOffset expiresAt) GenerateRefreshToken(
        IConfiguration configuration)
    {
        var refreshDays = configuration.GetValue<int>("Jwt:RefreshTokenDays", 30);
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
        return (raw, hash, DateTimeOffset.UtcNow.AddDays(refreshDays));
    }

    internal static UserProfileDto ToProfileDto(ApplicationUser user) => new(
        Id: user.Id,
        Email: user.Email!,
        EmailConfirmed: user.EmailConfirmed,
        FirstName: user.FirstName,
        LastName: user.LastName,
        PhoneNumber: user.PhoneNumber,
        ProfilePhotoUrl: user.ProfilePhotoUrl,
        MarketingOptIn: user.MarketingOptIn,
        CreatedAt: user.CreatedAt,
        LastLoginAt: user.LastLoginAt
    );

    internal static RefreshToken CreateRefreshTokenEntity(Guid userId, string hash, DateTimeOffset expiresAt, string? ip) =>
        new() { UserId = userId, TokenHash = hash, ExpiresAt = expiresAt, CreatedByIp = ip };
}
