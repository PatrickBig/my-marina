using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Identity;
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

        var tokenInfo = await AuthHelpers.BuildTokenInfoAsync(userManager, user, db, ct);
        var accessToken = jwtTokenService.GenerateAccessToken(tokenInfo);
        var (rawRefresh, hashedRefresh, expiresAt) = AuthHelpers.GenerateRefreshToken(configuration);

        db.RefreshTokens.Add(AuthHelpers.CreateRefreshTokenEntity(user.Id, hashedRefresh, expiresAt, command.IpAddress));
        await db.SaveChangesAsync(ct);

        return new AuthResponse(accessToken, rawRefresh, expiresAt, AuthHelpers.ToProfileDto(user));
    }
}
