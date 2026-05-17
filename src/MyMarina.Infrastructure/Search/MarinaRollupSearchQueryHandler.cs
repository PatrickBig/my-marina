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

        // Step 2: Get eligible slips within those marinas
        var baseSlipQuery = db.Slips
            .AsNoTracking()
            .Where(s => marinaIds.Contains(s.MarinaId));

        var filters = new SlipAvailabilityFilter.Filters(
            ArrivesAt:       query.ArrivesAt,
            DepartsAt:       query.DepartsAt,
            VesselLength:    query.VesselLength,
            VesselBeam:      query.VesselBeam,
            VesselDraft:     query.VesselDraft,
            SlipType:        query.SlipType,
            HasElectric:     query.HasElectric,
            HasWater:        query.HasWater,
            IsLease:         isLease,
            LeaseTermFilter: leaseTermFilter,
            IncludeDemo:     query.IncludeDemo
        );

        // Lightweight projections only — avoids materializing full Slip entities for aggregation
        var summaries = await SlipAvailabilityFilter.GetRollupSummariesAsync(db, baseSlipQuery, filters, ct);

        if (summaries.Count == 0) return [];

        // Step 3: Project marina details (no full entity load)
        var eligibleMarinaIds = summaries.Select(s => s.MarinaId).Distinct().ToList();
        var marinas = await db.Marinas
            .Where(m => eligibleMarinaIds.Contains(m.Id))
            .Select(m => new { m.Id, m.Name, m.AddressCity, m.AddressState, m.Latitude, m.Longitude })
            .ToDictionaryAsync(m => m.Id, ct);

        // Step 4: Group summaries by marina — summaries are lightweight (MarinaId, Price, RateKind, InstantBook)
        var centerLat = ((double)query.North + (double)query.South) / 2.0;
        var centerLon = ((double)query.East  + (double)query.West)  / 2.0;

        var rollups = summaries
            .GroupBy(s => s.MarinaId)
            .Where(g => marinas.ContainsKey(g.Key))
            .Select(g =>
            {
                var marina    = marinas[g.Key];
                var prices    = g.Select(s => s.Price).ToList();
                var rateKinds = g.Select(s => s.RateKind).Distinct().ToList();
                var rateKind  = rateKinds.Count > 1 ? "Mixed" : rateKinds[0];
                var distance  = GeoHelper.HaversineDistanceMiles(
                    centerLat, centerLon,
                    (double)marina.Latitude!.Value, (double)marina.Longitude!.Value);

                return new MarinaRollupResultDto(
                    MarinaId:                marina.Id,
                    MarinaName:              marina.Name,
                    City:                    marina.AddressCity,
                    State:                   marina.AddressState,
                    Latitude:                marina.Latitude!.Value,
                    Longitude:               marina.Longitude!.Value,
                    AvailableCount:          g.Count(),
                    MinPricePerNight:        prices.Min(),
                    MaxPricePerNight:        prices.Max(),
                    RateKind:                rateKind,
                    InstantBookAvailable:    g.Any(s => s.InstantBook),
                    DistanceMilesFromCenter: Math.Round(distance, 1)
                );
            })
            .OrderByDescending(r => r.AvailableCount)
            .ThenBy(r => r.DistanceMilesFromCenter)
            .Skip(query.Page * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        return rollups;
    }
}
