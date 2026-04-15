using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Boats;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Boats;

public class DeleteBoatCommandHandler(AppDbContext db) : ICommandHandler<DeleteBoatCommand>
{
    public async Task HandleAsync(DeleteBoatCommand command, CancellationToken ct = default)
    {
        var boat = await db.Boats.FirstOrDefaultAsync(b => b.Id == command.BoatId, ct)
            ?? throw new KeyNotFoundException($"Boat {command.BoatId} not found.");

        db.Boats.Remove(boat);
        await db.SaveChangesAsync(ct);
    }
}
