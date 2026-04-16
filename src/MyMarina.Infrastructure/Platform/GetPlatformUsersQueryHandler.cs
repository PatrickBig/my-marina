using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Platform;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Platform;

public class GetPlatformUsersQueryHandler(AppDbContext db)
    : IQueryHandler<GetPlatformUsersQuery, IReadOnlyList<PlatformUserDto>>
{
    public async Task<IReadOnlyList<PlatformUserDto>> HandleAsync(
        GetPlatformUsersQuery query, CancellationToken ct = default)
    {
        var q = db.Users.AsQueryable();

        if (query.TenantId.HasValue)
            q = q.Where(u => u.TenantId == query.TenantId.Value);

        if (query.Role.HasValue)
            q = q.Where(u => u.Role == query.Role.Value);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim().ToLower();
            q = q.Where(u =>
                u.Email!.ToLower().Contains(search) ||
                u.FirstName.ToLower().Contains(search) ||
                u.LastName.ToLower().Contains(search));
        }

        return await q
            .OrderBy(u => u.Email)
            .Select(u => new PlatformUserDto(
                u.Id,
                u.Email!,
                u.FirstName,
                u.LastName,
                u.Role,
                u.TenantId,
                u.TenantId != null
                    ? db.Tenants.Where(t => t.Id == u.TenantId).Select(t => t.Name).FirstOrDefault()
                    : null,
                u.MarinaId,
                u.MarinaId != null
                    ? db.Marinas.Where(m => m.Id == u.MarinaId).Select(m => m.Name).FirstOrDefault()
                    : null,
                u.IsActive,
                u.CreatedAt,
                u.LastLoginAt))
            .ToListAsync(ct);
    }
}
