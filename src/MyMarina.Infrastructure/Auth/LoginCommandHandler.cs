using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Auth;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Identity;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Auth;

public class LoginCommandHandler(
    UserManager<ApplicationUser> userManager,
    IJwtTokenService jwtTokenService,
    AppDbContext db) : ICommandHandler<LoginCommand, LoginResult>
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

        // For customer users, include all their CustomerAccountIds in the token so portal
        // endpoints can scope queries without an extra DB lookup per request.
        Guid? customerAccountId = null;
        IReadOnlyList<Guid>? customerAccountIds = null;
        if (user.Role == UserRole.Customer && user.TenantId.HasValue)
        {
            var accountIds = await db.CustomerAccountMembers
                .Where(m => m.UserId == user.Id && m.TenantId == user.TenantId.Value)
                .Select(m => m.CustomerAccountId)
                .ToListAsync(ct);

            if (accountIds.Count > 0)
            {
                customerAccountIds = accountIds.AsReadOnly();
                customerAccountId = accountIds[0]; // For backward compatibility
            }
        }

        var tokenInfo = new UserTokenInfo(
            UserId: user.Id,
            Email: user.Email!,
            FirstName: user.FirstName,
            LastName: user.LastName,
            Role: user.Role,
            TenantId: user.TenantId,
            MarinaId: user.MarinaId,
            CustomerAccountId: customerAccountId,
            CustomerAccountIds: customerAccountIds);

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
