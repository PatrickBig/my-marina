using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.AvailabilityWindows;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.AvailabilityWindows;

public class SetAvailabilityWindowStatusCommandHandler(AppDbContext db)
    : ICommandHandler<SetAvailabilityWindowStatusCommand, AvailabilityWindowDto>
{
    public async Task<AvailabilityWindowDto> HandleAsync(SetAvailabilityWindowStatusCommand command, CancellationToken ct = default)
    {
        var window = await db.AvailabilityWindows
            .Include(w => w.Slip)
            .FirstOrDefaultAsync(w => w.Id == command.Id && w.Slip.MarinaId == command.MarinaId, ct)
            ?? throw new KeyNotFoundException($"AvailabilityWindow {command.Id} not found.");

        if (command.Status == AvailabilityWindowStatus.FullyBooked)
            throw new InvalidOperationException("FullyBooked status is set automatically and cannot be assigned manually.");

        window.Status = command.Status;
        await db.SaveChangesAsync(ct);
        return AvailabilityWindowHelper.ToDto(window);
    }
}
