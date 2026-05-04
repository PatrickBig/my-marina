using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.BillingAccounts;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.BillingAccounts;

public class GetBillingAccountMembersQueryHandler(AppDbContext db)
    : IQueryHandler<GetBillingAccountMembersQuery, IReadOnlyList<BillingAccountMemberDto>>
{
    public async Task<IReadOnlyList<BillingAccountMemberDto>> HandleAsync(GetBillingAccountMembersQuery query, CancellationToken ct = default)
    {
        // Verify the account belongs to the requested marina
        var exists = await db.BillingAccounts
            .AnyAsync(a => a.Id == query.BillingAccountId && a.MarinaId == query.MarinaId, ct);
        if (!exists) throw new KeyNotFoundException($"BillingAccount {query.BillingAccountId} not found.");

        return await db.BillingAccountMembers
            .Where(m => m.BillingAccountId == query.BillingAccountId)
            .OrderBy(m => m.InvitedAt)
            .Join(db.Users, m => m.UserId, u => u.Id, (m, u) => new BillingAccountMemberDto(
                m.Id,
                m.BillingAccountId,
                m.UserId,
                u.Email ?? string.Empty,
                (u.FirstName + " " + u.LastName).Trim(),
                m.Role.ToString(),
                m.InvitedAt,
                m.AcceptedAt))
            .ToListAsync(ct);
    }
}
