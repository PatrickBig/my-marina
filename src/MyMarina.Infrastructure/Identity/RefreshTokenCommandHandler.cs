using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Identity;
using MyMarina.Domain.Entities;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Identity;

public class RefreshTokenCommandHandler(
    UserManager<ApplicationUser> userManager,
    IJwtTokenService jwtTokenService,
    AppDbContext db,
    IConfiguration configuration)
    : ICommandHandler<RefreshTokenCommand, AuthResponse>
{
    public async Task<AuthResponse> HandleAsync(RefreshTokenCommand command, CancellationToken ct = default)
    {
        var hash = HashToken(command.RefreshToken);
        var existing = await db.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct)
            ?? throw new UnauthorizedAccessException("Invalid refresh token.");

        if (!existing.IsActive)
        {
            if (existing.RevokedAt is null)
            {
                // Token reused — revoke all tokens for this user (reuse-detection)
                await RevokeAllUserTokens(existing.UserId, ct);
            }
            throw new UnauthorizedAccessException("Refresh token is no longer valid.");
        }

        var user = await userManager.FindByIdAsync(existing.UserId.ToString())
            ?? throw new UnauthorizedAccessException("User not found.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account is disabled.");

        // Rotate: revoke old, issue new
        existing.RevokedAt = DateTimeOffset.UtcNow;

        var (rawNew, hashedNew, expiresAt) = GenerateRefreshToken();
        var newToken = new RefreshToken
        {
            UserId      = user.Id,
            TokenHash   = hashedNew,
            ExpiresAt   = expiresAt,
            CreatedByIp = command.IpAddress,
        };
        existing.ReplacedByTokenId = newToken.Id;
        db.RefreshTokens.Add(newToken);
        await db.SaveChangesAsync(ct);

        var tokenInfo = await BuildTokenInfo(user);
        var accessToken = jwtTokenService.GenerateAccessToken(tokenInfo);

        return new AuthResponse(accessToken, rawNew, expiresAt, ToProfileDto(user));
    }

    private async Task RevokeAllUserTokens(Guid userId, CancellationToken ct)
    {
        var tokens = await db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ToListAsync(ct);
        foreach (var t in tokens)
            t.RevokedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    private async Task<UserTokenInfo> BuildTokenInfo(ApplicationUser user)
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

    private (string raw, string hash, DateTimeOffset expiresAt) GenerateRefreshToken()
    {
        var refreshDays = int.TryParse(configuration["Jwt:RefreshTokenDays"], out var d) ? d : 30;
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var hash = HashToken(raw);
        return (raw, hash, DateTimeOffset.UtcNow.AddDays(refreshDays));
    }

    private static string HashToken(string raw) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));

    private static UserProfileDto ToProfileDto(ApplicationUser user) => new(
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
}
