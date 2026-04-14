using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Slips;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Slips;

public class DeleteSlipCommandHandler(AppDbContext db) : ICommandHandler<DeleteSlipCommand>
{
    public async Task HandleAsync(DeleteSlipCommand command, CancellationToken ct = default)
    {
        var slip = await db.Slips.FirstOrDefaultAsync(s => s.Id == command.SlipId, ct)
            ?? throw new KeyNotFoundException($"Slip {command.SlipId} not found.");

        db.Slips.Remove(slip);
        await db.SaveChangesAsync(ct);
    }
}
