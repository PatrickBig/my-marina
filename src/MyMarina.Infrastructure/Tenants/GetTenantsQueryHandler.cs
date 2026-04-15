using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Tenants;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Tenants;

public class GetTenantsQueryHandler(AppDbContext db) : IQueryHandler<GetTenantsQuery, IReadOnlyList<TenantDto>>
{
    public async Task<IReadOnlyList<TenantDto>> HandleAsync(GetTenantsQuery query, CancellationToken ct = default)
    {
        return await db.Tenants
            .OrderBy(t => t.Name)
            .Select(t => new TenantDto(t.Id, t.Name, t.Slug, t.IsActive, t.SubscriptionTier, t.CreatedAt))
            .ToListAsync(ct);
    }
}
