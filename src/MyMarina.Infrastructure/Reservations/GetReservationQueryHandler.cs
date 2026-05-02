using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Reservations;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Reservations;

public class GetReservationQueryHandler(AppDbContext db)
    : IQueryHandler<GetReservationQuery, ReservationDto>
{
    public async Task<ReservationDto> HandleAsync(GetReservationQuery query, CancellationToken ct = default)
    {
        var reservation = await db.Reservations
            .Include(r => r.Slip)
            .Include(r => r.Vessel)
            .Include(r => r.AvailabilityWindow)
            .FirstOrDefaultAsync(r => r.Id == query.Id, ct)
            ?? throw new KeyNotFoundException($"Reservation {query.Id} not found.");

        // Access: boater who made it, or anyone with marina access (validated in controller)
        var marina = await db.Marinas.FindAsync([reservation.Slip.MarinaId], ct);
        return ReservationHelper.ToDtoWithMarina(reservation, marina?.Name ?? string.Empty);
    }
}
