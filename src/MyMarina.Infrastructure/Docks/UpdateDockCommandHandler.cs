using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Docks;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Docks;

public class UpdateDockCommandHandler(AppDbContext db) : ICommandHandler<UpdateDockCommand>
{
    public async Task HandleAsync(UpdateDockCommand command, CancellationToken ct = default)
    {
        var dock = await db.Docks.FirstOrDefaultAsync(d => d.Id == command.DockId, ct)
            ?? throw new KeyNotFoundException($"Dock {command.DockId} not found.");

        dock.Name = command.Name;
        dock.Description = command.Description;
        dock.SortOrder = command.SortOrder;
        dock.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
    }
}
