using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Auth;
using MyMarina.Domain.Common;
using MyMarina.Domain.Entities;
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

        var contexts = await GetAvailableContextsAsync(user, ct);
        if (contexts.Count == 0)
            throw new UnauthorizedAccessException("User has no available contexts.");

        // If only one context, issue token immediately
        if (contexts.Count == 1)
        {
            var context = contexts[0];
            var tokenInfo = BuildTokenInfo(user, context, hasMultipleContexts: false);
            var token = jwtTokenService.GenerateToken(tokenInfo);

            return new LoginResult(
                Token: token,
                ExpiresAt: DateTimeOffset.UtcNow.AddHours(1),
                UserId: user.Id,
                Email: user.Email!,
                FirstName: user.FirstName,
                LastName: user.LastName,
                Role: context.Role,
                TenantId: context.TenantId,
                MarinaId: context.MarinaId,
                AvailableContexts: contexts);
        }

        // Multiple contexts: return them for user to choose
        return new LoginResult(
            Token: null,
            ExpiresAt: DateTimeOffset.UtcNow.AddHours(1),
            UserId: user.Id,
            Email: user.Email!,
            FirstName: user.FirstName,
            LastName: user.LastName,
            Role: null,
            TenantId: null,
            MarinaId: null,
            AvailableContexts: contexts);
    }

    private async Task<List<AvailableContext>> GetAvailableContextsAsync(ApplicationUser user, CancellationToken ct)
    {
        var userContexts = await db.UserContexts
            .Where(uc => uc.UserId == user.Id)
            .Include(uc => uc.Role)
            .ToListAsync(ct);

        var contexts = new List<AvailableContext>();

        foreach (var userContext in userContexts)
        {
            var displayName = await BuildDisplayNameAsync(userContext, ct);

            contexts.Add(new AvailableContext(
                DisplayName: displayName,
                Role: userContext.Role?.Name ?? "Unknown",
                TenantId: userContext.TenantId,
                MarinaId: userContext.MarinaId,
                CustomerAccountId: userContext.CustomerAccountId));
        }

        return contexts;
    }

    private async Task<string> BuildDisplayNameAsync(UserContext userContext, CancellationToken ct)
    {
        var roleName = userContext.Role?.Name ?? "Unknown";

        // Platform admin sees all tenants
        if (roleName == Roles.PlatformAdmin)
        {
            var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == userContext.TenantId, ct);
            return $"{roleName} @ {tenant?.Name ?? "Unknown"}";
        }

        // Tenant owner sees tenant name
        if (roleName == Roles.TenantOwner)
        {
            var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == userContext.TenantId, ct);
            return tenant?.Name ?? "Unknown Tenant";
        }

        // Marina-scoped staff sees marina name
        if (userContext.MarinaId.HasValue)
        {
            var marina = await db.Marinas.FirstOrDefaultAsync(m => m.Id == userContext.MarinaId.Value, ct);
            return $"{roleName} @ {marina?.Name ?? "Unknown Marina"}";
        }

        // Customer sees account name
        if (roleName == Roles.Customer && userContext.CustomerAccountId.HasValue)
        {
            var account = await db.CustomerAccounts.FirstOrDefaultAsync(
                a => a.Id == userContext.CustomerAccountId.Value, ct);
            return $"Customer @ {account?.DisplayName ?? "Unknown Account"}";
        }

        return roleName;
    }

    private UserTokenInfo BuildTokenInfo(ApplicationUser user, AvailableContext context, bool hasMultipleContexts)
    {
        Guid? customerAccountId = null;
        IReadOnlyList<Guid>? customerAccountIds = null;

        if (context.Role == Roles.Customer && context.CustomerAccountId.HasValue)
        {
            customerAccountId = context.CustomerAccountId.Value;
            customerAccountIds = new[] { context.CustomerAccountId.Value }.AsReadOnly();
        }

        return new UserTokenInfo(
            UserId: user.Id,
            Email: user.Email!,
            FirstName: user.FirstName,
            LastName: user.LastName,
            Role: context.Role,
            TenantId: context.TenantId,
            MarinaId: context.MarinaId,
            CustomerAccountId: customerAccountId,
            CustomerAccountIds: customerAccountIds,
            HasMultipleContexts: hasMultipleContexts);
    }
}
