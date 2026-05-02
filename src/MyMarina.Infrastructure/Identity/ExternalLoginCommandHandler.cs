using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Identity;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Identity;

public class ExternalLoginCommandHandler(
    UserManager<ApplicationUser> userManager,
    IJwtTokenService jwtTokenService,
    AppDbContext db,
    IConfiguration configuration)
    : ICommandHandler<ExternalLoginCommand, AuthResponse>
{
    public async Task<AuthResponse> HandleAsync(ExternalLoginCommand command, CancellationToken ct = default)
    {
        // 1. Look up by external login (provider + providerKey)
        var user = await userManager.FindByLoginAsync(command.Provider, command.ProviderKey);

        if (user is null)
        {
            // 2. Email collision: existing account owns this email — require manual link
            if (command.Email is not null)
            {
                var existing = await userManager.FindByEmailAsync(command.Email);
                if (existing is not null)
                    throw new ExternalEmailCollisionException();
            }

            // 3. Register new user — email is confirmed because the provider confirmed it
            user = new ApplicationUser
            {
                Id             = Guid.CreateVersion7(),
                UserName       = command.Email ?? $"{command.Provider}_{command.ProviderKey}",
                Email          = command.Email,
                EmailConfirmed = command.Email is not null,
                FirstName      = command.FirstName ?? "",
                LastName       = command.LastName  ?? "",
            };

            var createResult = await userManager.CreateAsync(user);
            if (!createResult.Succeeded)
                throw new IdentityException(createResult.Errors);

            var loginInfo = new UserLoginInfo(command.Provider, command.ProviderKey, command.Provider);
            await userManager.AddLoginAsync(user, loginInfo);
        }

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account is disabled.");

        user.LastLoginAt = DateTimeOffset.UtcNow;
        await userManager.UpdateAsync(user);

        var tokenInfo = await AuthHelpers.BuildTokenInfoAsync(userManager, user);
        var accessToken = jwtTokenService.GenerateAccessToken(tokenInfo);
        var (rawRefresh, hashedRefresh, expiresAt) = AuthHelpers.GenerateRefreshToken(configuration);

        db.RefreshTokens.Add(AuthHelpers.CreateRefreshTokenEntity(user.Id, hashedRefresh, expiresAt, command.IpAddress));
        await db.SaveChangesAsync(ct);

        return new AuthResponse(accessToken, rawRefresh, expiresAt, AuthHelpers.ToProfileDto(user));
    }
}
