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

public class LoginCommandHandler(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IJwtTokenService jwtTokenService,
    AppDbContext db,
    IConfiguration configuration)
    : ICommandHandler<LoginCommand, AuthResponse>
{
    public async Task<AuthResponse> HandleAsync(LoginCommand command, CancellationToken ct = default)
    {
        var user = await userManager.FindByEmailAsync(command.Email)
            ?? throw new UnauthorizedAccessException("Invalid credentials.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account is disabled.");

        var result = await signInManager.CheckPasswordSignInAsync(user, command.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            if (result.IsLockedOut)
                throw new UnauthorizedAccessException("Account is locked. Try again later.");
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        user.LastLoginAt = DateTimeOffset.UtcNow;
        await userManager.UpdateAsync(user);

        var tokenInfo = await BuildTokenInfo(user);
        var accessToken = jwtTokenService.GenerateAccessToken(tokenInfo);
        var (rawRefresh, hashedRefresh, expiresAt) = GenerateRefreshToken();

        var refreshToken = new RefreshToken
        {
            UserId       = user.Id,
            TokenHash    = hashedRefresh,
            ExpiresAt    = expiresAt,
            CreatedByIp  = command.IpAddress,
        };
        db.RefreshTokens.Add(refreshToken);
        await db.SaveChangesAsync(ct);

        return new AuthResponse(accessToken, rawRefresh, expiresAt, ToProfileDto(user));
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
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));
        return (raw, hash, DateTimeOffset.UtcNow.AddDays(refreshDays));
    }

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
