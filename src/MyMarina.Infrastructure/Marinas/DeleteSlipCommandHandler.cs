using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Marinas;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Marinas;

public class DeleteSlipCommandHandler(AppDbContext db)
    : ICommandHandler<DeleteSlipCommand>
{
    public async Task HandleAsync(DeleteSlipCommand command, CancellationToken ct = default)
    {
        var slip = await db.Slips
            .FirstOrDefaultAsync(s => s.Id == command.SlipId && s.MarinaId == command.MarinaId, ct)
            ?? throw new KeyNotFoundException("Slip not found.");
        db.Slips.Remove(slip);
        await db.SaveChangesAsync(ct);
    }
}
