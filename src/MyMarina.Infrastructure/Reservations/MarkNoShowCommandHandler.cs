using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Reservations;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Reservations;

public class MarkNoShowCommandHandler(AppDbContext db)
    : ICommandHandler<MarkNoShowCommand, ReservationDto>
{
    public async Task<ReservationDto> HandleAsync(MarkNoShowCommand command, CancellationToken ct = default)
    {
        var reservation = await db.Reservations
            .Include(r => r.Slip)
            .Include(r => r.Vessel)
            .Include(r => r.AvailabilityWindow)
            .FirstOrDefaultAsync(r => r.Id == command.Id && r.Slip.MarinaId == command.MarinaId, ct)
            ?? throw new KeyNotFoundException($"Reservation {command.Id} not found.");

        if (reservation.Status != ReservationStatus.Confirmed)
            throw new InvalidOperationException("Only confirmed reservations can be marked as no-show.");

        reservation.Status = ReservationStatus.NoShow;

        await db.SaveChangesAsync(ct);

        var marina = await db.Marinas.FindAsync([reservation.Slip.MarinaId], ct);
        return ReservationHelper.ToDtoWithMarina(reservation, marina?.Name ?? string.Empty);
    }
}
