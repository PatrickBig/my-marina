using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Reservations;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Reservations;

public class GetMarinaReservationsQueryHandler(AppDbContext db)
    : IQueryHandler<GetMarinaReservationsQuery, IReadOnlyList<ReservationDto>>
{
    public async Task<IReadOnlyList<ReservationDto>> HandleAsync(GetMarinaReservationsQuery query, CancellationToken ct = default)
    {
        var marina = await db.Marinas.FindAsync([query.MarinaId], ct)
            ?? throw new KeyNotFoundException($"Marina {query.MarinaId} not found.");

        // Slip IDs belonging to this marina (as owner) OR where this marina is the host marina
        var ownedSlipIds = await db.Slips
            .Where(s => s.MarinaId == query.MarinaId || s.HostMarinaId == query.MarinaId)
            .Select(s => s.Id)
            .ToListAsync(ct);

        var q = db.Reservations
            .Include(r => r.Slip)
            .Include(r => r.Vessel)
            .Include(r => r.AvailabilityWindow)
            .Where(r => ownedSlipIds.Contains(r.SlipId))
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.StatusFilter)
            && Enum.TryParse<Domain.Enums.ReservationStatus>(query.StatusFilter, ignoreCase: true, out var status))
        {
            q = q.Where(r => r.Status == status);
        }

        var reservations = await q
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync(ct);

        return reservations
            .Select(r => ReservationHelper.ToDtoWithMarina(r, marina.Name))
            .ToList();
    }
}
