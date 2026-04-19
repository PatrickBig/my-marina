using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Auth;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Identity;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Auth;

public class ChooseContextCommandHandler(
    UserManager<ApplicationUser> userManager,
    IJwtTokenService jwtTokenService,
    AppDbContext db) : ICommandHandler<ChooseContextCommand, ContextToken>
{
    public async Task<ContextToken> HandleAsync(ChooseContextCommand command, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(command.UserId.ToString())
            ?? throw new UnauthorizedAccessException("User not found.");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("Account is disabled.");

        var context = command.Context;

        SubscriptionTier tier = SubscriptionTier.Free;
        if (context.TenantId != Guid.Empty)
        {
            var tenant = await db.Tenants.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == context.TenantId, ct);
            tier = tenant?.SubscriptionTier ?? SubscriptionTier.Free;
        }

        var tokenInfo = new UserTokenInfo(
            UserId: user.Id,
            Email: user.Email!,
            FirstName: user.FirstName,
            LastName: user.LastName,
            Role: context.Role,
            TenantId: context.TenantId,
            MarinaId: context.MarinaId,
            CustomerAccountId: context.CustomerAccountId,
            CustomerAccountIds: context.CustomerAccountId.HasValue
                ? new[] { context.CustomerAccountId.Value }.AsReadOnly()
                : null,
            HasMultipleContexts: true,
            SubscriptionTier: tier);

        var token = jwtTokenService.GenerateToken(tokenInfo);

        return new ContextToken(
            Token: token,
            ExpiresAt: DateTimeOffset.UtcNow.AddHours(1));
    }
}
