using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Identity;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Identity;

public class MeQueryHandler(
    UserManager<ApplicationUser> userManager,
    IUserContext userContext,
    AppDbContext db)
    : IQueryHandler<MeQuery, MeResponse>
{
    public async Task<MeResponse> HandleAsync(MeQuery query, CancellationToken ct = default)
    {
        if (!userContext.IsAuthenticated || userContext.UserId is null)
            throw new UnauthorizedAccessException();

        var user = await userManager.FindByIdAsync(userContext.UserId.Value.ToString())
            ?? throw new InvalidOperationException("User not found.");

        var marinaIds = userContext.Memberships
            .Where(m => m.MarinaId.HasValue)
            .Select(m => m.MarinaId!.Value)
            .Distinct()
            .ToList();

        var marinaNames = marinaIds.Count > 0
            ? await db.Marinas
                .Where(m => marinaIds.Contains(m.Id))
                .Select(m => new { m.Id, m.Name })
                .ToDictionaryAsync(m => m.Id, m => m.Name, ct)
            : [];

        var memberships = userContext.Memberships
            .Select(m => new MeMembershipDto(
                Scope: m.Scope,
                TenantId: m.TenantId,
                MarinaId: m.MarinaId,
                MarinaName: m.MarinaId.HasValue && marinaNames.TryGetValue(m.MarinaId.Value, out var name) ? name : null,
                Role: m.Role,
                Tier: m.Tier
            ))
            .ToList();

        return new MeResponse(
            Id: user.Id,
            Email: user.Email!,
            EmailConfirmed: user.EmailConfirmed,
            FirstName: user.FirstName,
            LastName: user.LastName,
            PhoneNumber: user.PhoneNumber,
            ProfilePhotoUrl: user.ProfilePhotoUrl,
            MarketingOptIn: user.MarketingOptIn,
            CreatedAt: user.CreatedAt,
            LastLoginAt: user.LastLoginAt,
            Memberships: memberships,
            BillingAccounts: userContext.BillingAccounts,
            IsPlatformOperator: userContext.IsPlatformOperator
        );
    }
}
