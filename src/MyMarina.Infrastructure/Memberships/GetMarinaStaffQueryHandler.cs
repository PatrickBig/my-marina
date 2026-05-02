using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Memberships;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Memberships;

public class GetMarinaStaffQueryHandler(AppDbContext db)
    : IQueryHandler<GetMarinaStaffQuery, IReadOnlyList<MembershipDto>>
{
    public async Task<IReadOnlyList<MembershipDto>> HandleAsync(GetMarinaStaffQuery query, CancellationToken ct = default)
    {
        var memberships = await db.Memberships
            .Where(m => m.MarinaId == query.MarinaId)
            .OrderBy(m => m.InvitedAt)
            .ToListAsync(ct);

        var userIds = memberships
            .Where(m => m.UserId != Guid.Empty)
            .Select(m => m.UserId)
            .Distinct()
            .ToList();

        var users = await db.Users
            .Where(u => userIds.Contains(u.Id))
            .Select(u => new { u.Id, u.Email, u.FirstName, u.LastName })
            .ToListAsync(ct);

        var userMap = users.ToDictionary(u => u.Id);

        return memberships.Select(m =>
        {
            userMap.TryGetValue(m.UserId, out var u);
            return MembershipMappers.ToDto(m, u?.Email, u?.FirstName, u?.LastName);
        }).ToList();
    }
}
