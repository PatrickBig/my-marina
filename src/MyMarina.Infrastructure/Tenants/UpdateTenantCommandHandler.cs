using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Tenants;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Tenants;

public class UpdateTenantCommandHandler(AppDbContext db) : ICommandHandler<UpdateTenantCommand>
{
    public async Task HandleAsync(UpdateTenantCommand command, CancellationToken ct = default)
    {
        var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == command.TenantId, ct)
            ?? throw new KeyNotFoundException($"Tenant {command.TenantId} not found.");

        tenant.Name = command.Name;
        tenant.IsActive = command.IsActive;
        tenant.SubscriptionTier = command.SubscriptionTier;

        await db.SaveChangesAsync(ct);
    }
}
