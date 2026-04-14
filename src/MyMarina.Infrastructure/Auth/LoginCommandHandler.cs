using Microsoft.AspNetCore.Identity;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Auth;
using MyMarina.Infrastructure.Identity;

namespace MyMarina.Infrastructure.Auth;

public class LoginCommandHandler(
    UserManager<ApplicationUser> userManager,
    IJwtTokenService jwtTokenService) : ICommandHandler<LoginCommand, LoginResult>
{
    public async Task<LoginResult> HandleAsync(LoginCommand command, CancellationToken ct = default)
    {
        var user = await userManager.FindByEmailAsync(command.Email)
            ?? throw new UnauthorizedAccessException("Invalid credentials.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account is disabled.");

        var passwordValid = await userManager.CheckPasswordAsync(user, command.Password);
        if (!passwordValid)
            throw new UnauthorizedAccessException("Invalid credentials.");

        user.LastLoginAt = DateTimeOffset.UtcNow;
        await userManager.UpdateAsync(user);

        var tokenInfo = new UserTokenInfo(
            UserId: user.Id,
            Email: user.Email!,
            FirstName: user.FirstName,
            LastName: user.LastName,
            Role: user.Role,
            TenantId: user.TenantId,
            MarinaId: user.MarinaId);

        var token = jwtTokenService.GenerateToken(tokenInfo);

        return new LoginResult(
            Token: token,
            ExpiresAt: DateTimeOffset.UtcNow.AddHours(1),
            UserId: user.Id,
            Email: user.Email!,
            FirstName: user.FirstName,
            LastName: user.LastName,
            Role: user.Role,
            TenantId: user.TenantId,
            MarinaId: user.MarinaId);
    }
}
