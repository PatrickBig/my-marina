using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Docks;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Docks;

public class DeleteDockCommandHandler(AppDbContext db) : ICommandHandler<DeleteDockCommand>
{
    public async Task HandleAsync(DeleteDockCommand command, CancellationToken ct = default)
    {
        var dock = await db.Docks.FirstOrDefaultAsync(d => d.Id == command.DockId, ct)
            ?? throw new KeyNotFoundException($"Dock {command.DockId} not found.");

        db.Docks.Remove(dock);
        await db.SaveChangesAsync(ct);
    }
}
