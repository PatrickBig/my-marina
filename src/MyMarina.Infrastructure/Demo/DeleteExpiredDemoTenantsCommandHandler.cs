using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Demo;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Demo;

/// <summary>
/// Deletes demo tenants whose TTL has expired. EF cascade deletes handle all child data.
/// Safe to run cross-tenant; uses IgnoreQueryFilters to bypass the tenant filter.
/// </summary>
public class DeleteExpiredDemoTenantsCommandHandler(AppDbContext db)
    : ICommandHandler<DeleteExpiredDemoTenantsCommand>
{
    public async Task HandleAsync(DeleteExpiredDemoTenantsCommand command, CancellationToken ct = default)
    {
        var expired = await db.Tenants
            .IgnoreQueryFilters()
            .Where(t => t.IsDemo && t.DemoExpiresAt != null && t.DemoExpiresAt < DateTimeOffset.UtcNow)
            .ToListAsync(ct);

        if (expired.Count == 0) return;

        db.Tenants.RemoveRange(expired);
        await db.SaveChangesAsync(ct);
    }
}
