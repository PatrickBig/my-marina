using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Reservations;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Reservations;

public class GetMyTripsQueryHandler(AppDbContext db)
    : IQueryHandler<GetMyTripsQuery, IReadOnlyList<ReservationDto>>
{
    public async Task<IReadOnlyList<ReservationDto>> HandleAsync(GetMyTripsQuery query, CancellationToken ct = default)
    {
        var q = db.Reservations
            .Include(r => r.Slip)
            .Include(r => r.Vessel)
            .Include(r => r.AvailabilityWindow)
            .Where(r => r.BoaterUserId == query.BoaterUserId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.StatusFilter)
            && Enum.TryParse<Domain.Enums.ReservationStatus>(query.StatusFilter, ignoreCase: true, out var status))
        {
            q = q.Where(r => r.Status == status);
        }

        var reservations = await q
            .OrderByDescending(r => r.ArrivesAt)
            .ToListAsync(ct);

        // Load marina names
        var marinaIds = reservations.Select(r => r.Slip.MarinaId).Distinct().ToList();
        var marinas   = await db.Marinas
            .Where(m => marinaIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id, m => m.Name, ct);

        return reservations
            .Select(r => ReservationHelper.ToDtoWithMarina(r, marinas.GetValueOrDefault(r.Slip.MarinaId) ?? string.Empty))
            .ToList();
    }
}
