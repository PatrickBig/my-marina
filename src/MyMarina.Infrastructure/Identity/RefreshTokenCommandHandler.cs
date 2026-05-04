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
        var hash = AuthHelpers.HashToken(command.RefreshToken);
        var existing = await db.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct)
            ?? throw new UnauthorizedAccessException("Invalid refresh token.");

        if (!existing.IsActive)
        {
            if (existing.RevokedAt is null)
                await RevokeAllUserTokens(existing.UserId, ct);
            throw new UnauthorizedAccessException("Refresh token is no longer valid.");
        }

        var user = await userManager.FindByIdAsync(existing.UserId.ToString())
            ?? throw new UnauthorizedAccessException("User not found.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account is disabled.");

        existing.RevokedAt = DateTimeOffset.UtcNow;

        var (rawNew, hashedNew, expiresAt) = AuthHelpers.GenerateRefreshToken(configuration);
        var newToken = AuthHelpers.CreateRefreshTokenEntity(user.Id, hashedNew, expiresAt, command.IpAddress);
        existing.ReplacedByTokenId = newToken.Id;
        db.RefreshTokens.Add(newToken);
        await db.SaveChangesAsync(ct);

        var tokenInfo = await AuthHelpers.BuildTokenInfoAsync(userManager, user, db, ct);
        var accessToken = jwtTokenService.GenerateAccessToken(tokenInfo);

        return new AuthResponse(accessToken, rawNew, expiresAt, AuthHelpers.ToProfileDto(user));
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
}
