using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Memberships;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Memberships;

public class GetMyMembershipsQueryHandler(AppDbContext db)
    : IQueryHandler<GetMyMembershipsQuery, IReadOnlyList<MembershipDto>>
{
    public async Task<IReadOnlyList<MembershipDto>> HandleAsync(GetMyMembershipsQuery query, CancellationToken ct = default)
    {
        var memberships = await db.Memberships
            .Where(m => m.UserId == query.UserId)
            .Include(m => m.Tenant)
            .OrderBy(m => m.InvitedAt)
            .ToListAsync(ct);

        return memberships.Select(m => MembershipMappers.ToDto(m, null, null, null)).ToList();
    }
}
