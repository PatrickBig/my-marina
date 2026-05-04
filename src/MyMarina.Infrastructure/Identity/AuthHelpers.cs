using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Identity;
using MyMarina.Domain.Entities;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Identity;

internal static class AuthHelpers
{
    internal static async Task<UserTokenInfo> BuildTokenInfoAsync(
        UserManager<ApplicationUser> userManager,
        ApplicationUser user,
        AppDbContext db,
        CancellationToken ct = default)
    {
        var isPlatformOp = await userManager.IsInRoleAsync(user, "PlatformOperator");

        var memberships = await db.Memberships
            .Where(m => m.UserId == user.Id && m.AcceptedAt != null)
            .Include(m => m.Tenant)
            .ToListAsync(ct);

        var membershipClaims = memberships.Select(m => new MembershipClaim(
            Scope: m.Scope,
            TenantId: m.TenantId,
            MarinaId: m.MarinaId,
            Role: m.Role,
            Tier: m.Tenant.SubscriptionTier.ToString()
        )).ToList();

        return new UserTokenInfo(
            UserId: user.Id,
            Email: user.Email!,
            EmailVerified: user.EmailConfirmed,
            FirstName: user.FirstName,
            LastName: user.LastName,
            IsPlatformOperator: isPlatformOp,
            IsDemo: false,
            Memberships: membershipClaims,
            BillingAccounts: []
        );
    }

    internal static (string raw, string hash, DateTimeOffset expiresAt) GenerateRefreshToken(
        IConfiguration configuration)
    {
        var refreshDays = configuration.GetValue<int>("Jwt:RefreshTokenDays", 30);
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var hash = HashToken(raw);
        return (raw, hash, DateTimeOffset.UtcNow.AddDays(refreshDays));
    }

    internal static string HashToken(string raw) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));

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
