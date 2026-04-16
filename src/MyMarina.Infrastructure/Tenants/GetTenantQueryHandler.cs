using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Tenants;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Tenants;

public class GetTenantQueryHandler(AppDbContext db) : IQueryHandler<GetTenantQuery, TenantDetailDto?>
{
    public async Task<TenantDetailDto?> HandleAsync(GetTenantQuery query, CancellationToken ct = default)
    {
        var tenant = await db.Tenants
            .FirstOrDefaultAsync(t => t.Id == query.TenantId, ct);

        if (tenant is null) return null;

        var marinas = await db.Marinas
            .Where(m => m.TenantId == query.TenantId)
            .OrderBy(m => m.Name)
            .Select(m => new TenantMarinaDto(m.Id, m.Name, m.CreatedAt))
            .ToListAsync(ct);

        var owner = await db.Users
            .Where(u => u.TenantId == query.TenantId && u.Role == UserRole.MarinaOwner)
            .OrderBy(u => u.CreatedAt)
            .Select(u => new TenantOwnerDto(u.Id, u.Email!, u.FirstName, u.LastName, u.IsActive))
            .FirstOrDefaultAsync(ct);

        return new TenantDetailDto(
            tenant.Id,
            tenant.Name,
            tenant.Slug,
            tenant.IsActive,
            tenant.SubscriptionTier,
            tenant.CreatedAt,
            marinas,
            owner);
    }
}
