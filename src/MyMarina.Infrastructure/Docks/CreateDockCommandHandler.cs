using MyMarina.Application.Abstractions;
using MyMarina.Application.Docks;
using MyMarina.Domain.Entities;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Docks;

public class CreateDockCommandHandler(
    AppDbContext db,
    ITenantContext tenantContext) : ICommandHandler<CreateDockCommand, Guid>
{
    public async Task<Guid> HandleAsync(CreateDockCommand command, CancellationToken ct = default)
    {
        var dock = new Dock
        {
            TenantId = tenantContext.TenantId,
            MarinaId = command.MarinaId,
            Name = command.Name,
            Description = command.Description,
            SortOrder = command.SortOrder,
        };

        db.Docks.Add(dock);
        await db.SaveChangesAsync(ct);

        return dock.Id;
    }
}
