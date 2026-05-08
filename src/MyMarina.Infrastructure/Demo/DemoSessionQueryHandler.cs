using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Demo;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Demo;

public class DemoSessionQueryHandler(AppDbContext db, IJwtTokenService jwt)
    : IQueryHandler<DemoSessionQuery, DemoSessionResponse>
{
    const int DemoExpiryMinutes = 30;

    public async Task<DemoSessionResponse> HandleAsync(DemoSessionQuery query, CancellationToken ct = default)
    {
        var memberships = await db.Memberships
            .Where(m => m.UserId == DemoSeedScript.DemoUserId && m.AcceptedAt != null)
            .Include(m => m.Tenant)
            .ToListAsync(ct);

        if (memberships.Count == 0)
            throw new InvalidOperationException("Demo data has not been seeded. Run --setup first.");

        var membershipClaims = memberships
            .Select(m => new MembershipClaim(
                m.Scope,
                m.TenantId,
                m.MarinaId,
                m.Role,
                m.Tenant.SubscriptionTier.ToString()))
            .ToList();

        var tokenInfo = new UserTokenInfo(
            UserId:            DemoSeedScript.DemoUserId,
            Email:             "demo@mymarina.org",
            EmailVerified:     true,
            FirstName:         "Demo",
            LastName:          "User",
            IsPlatformOperator: false,
            IsDemo:            true,
            Memberships:       membershipClaims,
            BillingAccounts:   []);

        var accessToken = jwt.GenerateAccessToken(tokenInfo, expiryMinutesOverride: DemoExpiryMinutes);
        var expiresAt   = DateTimeOffset.UtcNow.AddMinutes(DemoExpiryMinutes).ToString("O");

        return new DemoSessionResponse(accessToken, expiresAt);
    }
}
