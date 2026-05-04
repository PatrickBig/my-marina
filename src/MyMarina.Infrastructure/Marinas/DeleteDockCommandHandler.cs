using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Marinas;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Marinas;

public class DeleteDockCommandHandler(AppDbContext db)
    : ICommandHandler<DeleteDockCommand>
{
    public async Task HandleAsync(DeleteDockCommand command, CancellationToken ct = default)
    {
        var dock = await db.Docks
            .FirstOrDefaultAsync(d => d.Id == command.DockId && d.MarinaId == command.MarinaId, ct)
            ?? throw new KeyNotFoundException("Dock not found.");
        db.Docks.Remove(dock);
        await db.SaveChangesAsync(ct);
    }
}
