using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Marinas;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Marinas;

public class DeleteDraftMarinaCommandHandler(AppDbContext db)
    : ICommandHandler<DeleteDraftMarinaCommand>
{
    public async Task HandleAsync(DeleteDraftMarinaCommand command, CancellationToken ct = default)
    {
        var marina = await db.Marinas
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.Id == command.MarinaId, ct)
            ?? throw new KeyNotFoundException("Marina not found.");

        if (marina.IsSetupComplete)
            throw new InvalidOperationException("Only draft marinas can be deleted this way.");

        // Cascade: slips, docks, memberships — EF cascade delete handles the rest
        var slips = await db.Slips.Where(s => s.MarinaId == command.MarinaId).ToListAsync(ct);
        db.Slips.RemoveRange(slips);

        var docks = await db.Docks.Where(d => d.MarinaId == command.MarinaId).ToListAsync(ct);
        db.Docks.RemoveRange(docks);

        var memberships = await db.Memberships.Where(m => m.MarinaId == command.MarinaId).ToListAsync(ct);
        db.Memberships.RemoveRange(memberships);

        db.Marinas.Remove(marina);
        await db.SaveChangesAsync(ct);
    }
}
