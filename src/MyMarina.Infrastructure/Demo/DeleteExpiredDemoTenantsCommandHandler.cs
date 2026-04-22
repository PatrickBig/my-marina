using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Demo;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Demo;

/// <summary>
/// Deletes demo tenants whose TTL has expired. EF cascade deletes handle all child data.
/// Also deletes ApplicationUser rows stamped with an ExpiresAt in the past — covers demo
/// users (and any future expiring-user scenario) which are not parented to a tenant row.
/// Safe to run cross-tenant; uses IgnoreQueryFilters to bypass the tenant filter.
/// </summary>
public class DeleteExpiredDemoTenantsCommandHandler(AppDbContext db)
    : ICommandHandler<DeleteExpiredDemoTenantsCommand>
{
    public async Task HandleAsync(DeleteExpiredDemoTenantsCommand command, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;

        await db.Tenants
            .IgnoreQueryFilters()
            .Where(t => t.IsDemo && t.DemoExpiresAt != null && t.DemoExpiresAt < now)
            .ExecuteDeleteAsync(ct);

        await db.Users
            .Where(u => u.ExpiresAt != null && u.ExpiresAt < now)
            .ExecuteDeleteAsync(ct);
    }
}
