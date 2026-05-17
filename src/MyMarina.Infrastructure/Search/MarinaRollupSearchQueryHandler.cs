using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Search;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Search;

public class MarinaRollupSearchQueryHandler(AppDbContext db)
    : IQueryHandler<MarinaRollupSearchQuery, IReadOnlyList<MarinaRollupResultDto>>
{
    public async Task<IReadOnlyList<MarinaRollupResultDto>> HandleAsync(
        MarinaRollupSearchQuery query, CancellationToken ct = default)
    {
        bool isLease = string.Equals(query.ListingKind, "Lease", StringComparison.OrdinalIgnoreCase);

        LeaseTerm? leaseTermFilter = null;
        if (isLease && !string.IsNullOrWhiteSpace(query.LeaseTerm)
            && Enum.TryParse<LeaseTerm>(query.LeaseTerm, ignoreCase: true, out var parsedTerm))
            leaseTermFilter = parsedTerm;

        // Step 1: Find marinas within the bounding box that aren't demo (unless demo session)
        var marinaIds = await db.Marinas
            .Include(m => m.Tenant)
            .Where(m => m.Latitude  >= query.South && m.Latitude  <= query.North
                     && m.Longitude >= query.West  && m.Longitude <= query.East
                     && (query.IncludeDemo || !m.Tenant.IsDemo))
            .Select(m => m.Id)
            .ToListAsync(ct);

        if (marinaIds.Count == 0) return [];

        // Step 2: Build the slip filter predicate
        SlipType slipTypeFilter = default;
        bool hasSlipTypeFilter = !string.IsNullOrWhiteSpace(query.SlipType)
            && Enum.TryParse(query.SlipType, ignoreCase: true, out slipTypeFilter);

        var today = DateOnly.FromDateTime(DateTime.Today);

        var slipQ = db.Slips
            .Where(s => s.Status == SlipStatus.Active
                     && marinaIds.Contains(s.MarinaId)
                     && (query.VesselLength == null || s.MaxLength >= query.VesselLength)
                     && (query.VesselBeam   == null || s.MaxBeam   >= query.VesselBeam)
                     && (query.VesselDraft  == null || s.MaxDraft  >= query.VesselDraft)
                     && (!hasSlipTypeFilter || s.SlipType == slipTypeFilter)
                     && (query.HasElectric == null || s.HasElectric == query.HasElectric)
                     && (query.HasWater    == null || s.HasWater    == query.HasWater)
                     && (query.HasPumpOut  == null || !query.HasPumpOut.Value || s.HasPumpOut)
                     && (query.IsAnyCovered == null || !query.IsAnyCovered.Value || s.IsCovered));

        // Step 3: Apply availability EXISTS predicates
        if (isLease)
        {
            DateTime? desiredStart = query.ArrivesAt.HasValue
                ? query.ArrivesAt.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
                : (DateTime?)null;

            slipQ = slipQ.Where(s =>
                // No active assignment
                !db.SlipAssignments.Any(a => a.SlipId == s.Id
                    && (a.EndDate == null || a.EndDate >= today))
                &&
                // Has a matching window OR a matching default rate
                (db.AvailabilityWindows.Any(w => w.SlipId == s.Id
                    && w.Status == AvailabilityWindowStatus.Open
                    && w.ListingKind == ListingKind.Lease
                    && (leaseTermFilter == null || w.LeaseTerm == leaseTermFilter)
                    && (desiredStart == null || (w.StartsAt <= desiredStart && w.EndsAt >= desiredStart)))
                 || (s.DefaultLeaseRateKind != null
                    && (leaseTermFilter == null || s.DefaultLeaseTerm == leaseTermFilter))));
        }
        else
        {
            var arrivesAt = (query.ArrivesAt ?? today).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var departsAt = (query.DepartsAt ?? query.ArrivesAt?.AddDays(1) ?? today.AddDays(1))
                                .ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var arrivesDateOnly = DateOnly.FromDateTime(arrivesAt);
            var departsDateOnly = DateOnly.FromDateTime(departsAt);

            slipQ = slipQ.Where(s =>
                // Path A: open transient window covering the stay
                db.AvailabilityWindows.Any(w => w.SlipId == s.Id
                    && w.Status == AvailabilityWindowStatus.Open
                    && w.ListingKind == ListingKind.Transient
                    && w.StartsAt <= arrivesAt
                    && w.EndsAt   >= departsAt)
                ||
                // Path B: direct default rate with no conflicts
                (s.DefaultTransientRateKind != null
                 && !db.SlipAssignments.Any(a => a.SlipId == s.Id
                        && a.StartDate <= departsDateOnly
                        && (a.EndDate == null || a.EndDate >= arrivesDateOnly))
                 && !db.Reservations.Any(r => r.SlipId == s.Id
                        && r.ArrivesAt < departsAt
                        && r.DepartsAt > arrivesAt
                        && r.Status != ReservationStatus.Declined
                        && r.Status != ReservationStatus.Cancelled)));

            // Step 4: Apply price filter EXISTS predicate (transient)
            if (query.PriceMin.HasValue || query.PriceMax.HasValue)
            {
                slipQ = slipQ.Where(s =>
                    db.AvailabilityWindows.Any(w => w.SlipId == s.Id
                        && w.Status == AvailabilityWindowStatus.Open
                        && w.ListingKind == ListingKind.Transient
                        && w.StartsAt <= arrivesAt
                        && w.EndsAt   >= departsAt
                        && (query.PriceMin == null || w.BasePricePerNight >= query.PriceMin)
                        && (query.PriceMax == null || w.BasePricePerNight <= query.PriceMax))
                    || (s.DefaultTransientRateKind != null
                        && (query.PriceMin == null || s.DefaultTransientBaseRate >= query.PriceMin)
                        && (query.PriceMax == null || s.DefaultTransientBaseRate <= query.PriceMax)));
            }
        }

        // Apply lease price filter (after the lease availability block)
        if (isLease && (query.PriceMin.HasValue || query.PriceMax.HasValue))
        {
            DateTime? desiredStart = query.ArrivesAt.HasValue
                ? query.ArrivesAt.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
                : (DateTime?)null;

            slipQ = slipQ.Where(s =>
                db.AvailabilityWindows.Any(w => w.SlipId == s.Id
                    && w.Status == AvailabilityWindowStatus.Open
                    && w.ListingKind == ListingKind.Lease
                    && (leaseTermFilter == null || w.LeaseTerm == leaseTermFilter)
                    && (desiredStart == null || (w.StartsAt <= desiredStart && w.EndsAt >= desiredStart))
                    && (query.PriceMin == null || w.BasePricePerNight >= query.PriceMin)
                    && (query.PriceMax == null || w.BasePricePerNight <= query.PriceMax))
                || (s.DefaultLeaseRateKind != null
                    && (leaseTermFilter == null || s.DefaultLeaseTerm == leaseTermFilter)
                    && (query.PriceMin == null || s.DefaultLeaseBaseRate >= query.PriceMin)
                    && (query.PriceMax == null || s.DefaultLeaseBaseRate <= query.PriceMax)));
        }

        // Step 5: Single DB-level GROUP BY — count + instant-book + amenity flags per marina
        // Amenity booleans (HasPumpOut, HasElectric, IsAnyCovered) are computed as ANY aggregates
        // in the same GROUP BY pass as AvailableCount — no extra query passes.
        List<RollupRow> rollupData;

        if (isLease)
        {
            var rows = await slipQ
                .GroupBy(s => s.MarinaId)
                .Select(g => new
                {
                    MarinaId     = g.Key,
                    Count        = g.Count(),
                    HasPumpOut   = g.Any(s => s.HasPumpOut),
                    HasElectric  = g.Any(s => s.HasElectric),
                    IsAnyCovered = g.Any(s => s.IsCovered),
                })
                .ToListAsync(ct);
            rollupData = rows.Select(r => new RollupRow(r.MarinaId, r.Count, false, r.HasPumpOut, r.HasElectric, r.IsAnyCovered)).ToList();
        }
        else
        {
            var arrivesAt = (query.ArrivesAt ?? today).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var departsAt = (query.DepartsAt ?? query.ArrivesAt?.AddDays(1) ?? today.AddDays(1))
                                .ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

            var rows = await slipQ
                .GroupBy(s => s.MarinaId)
                .Select(g => new
                {
                    MarinaId     = g.Key,
                    Count        = g.Count(),
                    InstantBook  = g.Any(s =>
                        db.AvailabilityWindows.Any(w => w.SlipId == s.Id
                            && w.Status == AvailabilityWindowStatus.Open
                            && w.ListingKind == ListingKind.Transient
                            && w.StartsAt <= arrivesAt
                            && w.EndsAt   >= departsAt
                            && w.InstantBook)
                        || s.DefaultTransientRateKind != null),
                    HasPumpOut   = g.Any(s => s.HasPumpOut),
                    HasElectric  = g.Any(s => s.HasElectric),
                    IsAnyCovered = g.Any(s => s.IsCovered),
                })
                .ToListAsync(ct);
            rollupData = rows.Select(r => new RollupRow(r.MarinaId, r.Count, r.InstantBook, r.HasPumpOut, r.HasElectric, r.IsAnyCovered)).ToList();
        }

        // Apply InstantBookOnly in memory — bounded by result count (marina count, not slip count)
        if (query.InstantBookOnly == true)
            rollupData = rollupData.Where(r => r.InstantBook).ToList();

        if (rollupData.Count == 0) return [];

        // Step 6: Load marina details for matching marinas (bounded by result count, not slip count)
        var eligibleMarinaIds = rollupData.Select(r => r.MarinaId).Distinct().ToList();
        var marinas = await db.Marinas
            .Where(m => eligibleMarinaIds.Contains(m.Id))
            .Select(m => new { m.Id, m.Name, m.AddressCity, m.AddressState, m.Latitude, m.Longitude })
            .ToDictionaryAsync(m => m.Id, ct);

        var centerLat = ((double)query.North + (double)query.South) / 2.0;
        var centerLon = ((double)query.East  + (double)query.West)  / 2.0;

        var rollups = rollupData
            .Where(r => marinas.ContainsKey(r.MarinaId))
            .Select(r =>
            {
                var marina   = marinas[r.MarinaId];
                var distance = GeoHelper.HaversineDistanceMiles(
                    centerLat, centerLon,
                    (double)marina.Latitude!.Value, (double)marina.Longitude!.Value);

                return new MarinaRollupResultDto(
                    MarinaId:                marina.Id,
                    MarinaName:              marina.Name,
                    City:                    marina.AddressCity,
                    State:                   marina.AddressState,
                    Latitude:                marina.Latitude!.Value,
                    Longitude:               marina.Longitude!.Value,
                    AvailableCount:          r.Count,
                    InstantBookAvailable:    r.InstantBook,
                    DistanceMilesFromCenter: Math.Round(distance, 1),
                    PhotoUrl:                null,
                    HasPumpOut:              r.HasPumpOut,
                    HasElectric:             r.HasElectric,
                    IsAnyCovered:            r.IsAnyCovered
                );
            })
            .OrderByDescending(r => r.AvailableCount)
            .ThenBy(r => r.DistanceMilesFromCenter)
            .Skip(query.Page * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        return rollups;
    }

    private sealed record RollupRow(
        Guid MarinaId,
        int Count,
        bool InstantBook,
        bool HasPumpOut,
        bool HasElectric,
        bool IsAnyCovered
    );
}
