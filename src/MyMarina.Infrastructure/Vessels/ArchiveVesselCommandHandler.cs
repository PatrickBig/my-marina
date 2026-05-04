using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Vessels;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Vessels;

public class ArchiveVesselCommandHandler(AppDbContext db)
    : ICommandHandler<ArchiveVesselCommand>
{
    public async Task HandleAsync(ArchiveVesselCommand command, CancellationToken ct = default)
    {
        var vessel = await db.Vessels
            .FirstOrDefaultAsync(v => v.Id == command.Id && v.OwnerUserId == command.OwnerId, ct)
            ?? throw new KeyNotFoundException("Vessel not found.");

        vessel.IsArchived = true;
        await db.SaveChangesAsync(ct);
    }
}
