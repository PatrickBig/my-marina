using Microsoft.Extensions.Options;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Demo;
using MyMarina.Domain.Entities;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Demo;

public class ProvisionDemoTenantCommandHandler(
    AppDbContext db,
    DemoSeedScript seedScript,
    IOptions<DemoOptions> options) : ICommandHandler<ProvisionDemoTenantCommand, ProvisionDemoTenantResult>
{
    public async Task<ProvisionDemoTenantResult> HandleAsync(
        ProvisionDemoTenantCommand command, CancellationToken ct = default)
    {
        var tenantId = Guid.CreateVersion7();
        var tenant = new Tenant
        {
            Id = tenantId,
            Name = $"Demo Tenant {tenantId.ToString("N")[..8]}",
            Slug = $"demo-{tenantId:N}",
            SubscriptionTier = command.Tier,
            IsDemo = true,
            DemoExpiresAt = DateTimeOffset.UtcNow.AddMinutes(options.Value.TtlMinutes),
        };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync(ct);

        var seed = await seedScript.SeedAsync(tenantId, ct);

        return new ProvisionDemoTenantResult(
            TenantId: tenantId,
            OperatorUserId: seed.OperatorUserId,
            CustomerUserId: seed.CustomerUserId,
            CustomerAccountId: seed.CustomerAccountId);
    }
}
