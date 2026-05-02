using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Search;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Search;

public class SearchSlipsQueryHandler(AppDbContext db)
    : IQueryHandler<SearchSlipsQuery, IReadOnlyList<SlipSearchResultDto>>
{
    public async Task<IReadOnlyList<SlipSearchResultDto>> HandleAsync(SearchSlipsQuery query, CancellationToken ct = default)
    {
        var arrivesAt = query.ArrivesAt.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var departsAt = query.DepartsAt.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        // Step 1 — slip IDs that have at least one Open window covering the requested range
        var eligibleSlipIds = await db.AvailabilityWindows
            .Where(w => w.Status == AvailabilityWindowStatus.Open
                     && w.StartsAt <= arrivesAt
                     && w.EndsAt   >= departsAt)
            .Select(w => w.SlipId)
            .Distinct()
            .ToListAsync(ct);

        if (eligibleSlipIds.Count == 0)
            return [];

        // Step 2 — filter slips: active, vessel-fit, optional amenity/type filters
        SlipType slipTypeFilter = default;
        bool hasSlipTypeFilter = !string.IsNullOrWhiteSpace(query.SlipType)
            && Enum.TryParse(query.SlipType, ignoreCase: true, out slipTypeFilter);

        var slips = await db.Slips
            .Where(s => eligibleSlipIds.Contains(s.Id)
                     && s.Status == SlipStatus.Active
                     && (query.VesselLength == null || s.MaxLength >= query.VesselLength)
                     && (query.VesselBeam   == null || s.MaxBeam   >= query.VesselBeam)
                     && (query.VesselDraft  == null || s.MaxDraft  >= query.VesselDraft)
                     && (!hasSlipTypeFilter || s.SlipType == slipTypeFilter)
                     && (query.HasElectric == null || s.HasElectric == query.HasElectric)
                     && (query.HasWater    == null || s.HasWater    == query.HasWater))
            .ToListAsync(ct);

        if (slips.Count == 0)
            return [];

        // Step 3 — load marinas (+ tenants for demo filter)
        var marinaIds = slips.Select(s => s.MarinaId).Distinct().ToList();
        var marinas = await db.Marinas
            .Include(m => m.Tenant)
            .Where(m => marinaIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id, ct);

        // Step 4 — demo filter + effective lat/lon + bounding box
        var (minLat, maxLat, minLon, maxLon) = GeoHelper.BoundingBox(query.Latitude, query.Longitude, query.RadiusMiles);

        var candidates = slips
            .Where(s => marinas.TryGetValue(s.MarinaId, out var m) && (query.IncludeDemo || !m.Tenant.IsDemo))
            .Select(s =>
            {
                var m = marinas[s.MarinaId];
                var lat = s.Latitude ?? m.Latitude;
                var lon = s.Longitude ?? m.Longitude;
                return (slip: s, marina: m, lat, lon);
            })
            .Where(x => x.lat != null && x.lon != null
                     && x.lat >= minLat && x.lat <= maxLat
                     && x.lon >= minLon && x.lon <= maxLon)
            .Select(x => (
                x.slip, x.marina,
                lat: x.lat!.Value,
                lon: x.lon!.Value,
                distance: GeoHelper.HaversineDistanceMiles(
                    (double)query.Latitude, (double)query.Longitude,
                    (double)x.lat!.Value,   (double)x.lon!.Value)
            ))
            .Where(x => x.distance <= (double)query.RadiusMiles)
            .ToList();

        if (candidates.Count == 0)
            return [];

        // Step 5 — load best (cheapest) window per candidate slip
        var candidateSlipIds = candidates.Select(x => x.slip.Id).ToList();
        var allWindows = await db.AvailabilityWindows
            .Where(w => candidateSlipIds.Contains(w.SlipId)
                     && w.Status == AvailabilityWindowStatus.Open
                     && w.StartsAt <= arrivesAt
                     && w.EndsAt   >= departsAt)
            .ToListAsync(ct);

        var bestWindows = allWindows
            .GroupBy(w => w.SlipId)
            .ToDictionary(g => g.Key, g => g.OrderBy(w => w.BasePricePerNight).First());

        // Step 6 — sort, paginate, project
        return candidates
            .Where(x => bestWindows.ContainsKey(x.slip.Id))
            .OrderBy(x => x.distance)
            .ThenBy(x => bestWindows[x.slip.Id].BasePricePerNight)
            .Skip(query.Page * query.PageSize)
            .Take(query.PageSize)
            .Select(x =>
            {
                var w = bestWindows[x.slip.Id];
                return new SlipSearchResultDto(
                    SlipId:           x.slip.Id,
                    SlipName:         x.slip.Name,
                    SlipType:         x.slip.SlipType.ToString(),
                    MaxLength:        x.slip.MaxLength,
                    MaxBeam:          x.slip.MaxBeam,
                    MaxDraft:         x.slip.MaxDraft,
                    HasElectric:      x.slip.HasElectric,
                    HasWater:         x.slip.HasWater,
                    Latitude:         x.lat,
                    Longitude:        x.lon,
                    MarinaId:         x.marina.Id,
                    MarinaName:       x.marina.Name,
                    MarinaCity:       x.marina.AddressCity,
                    MarinaState:      x.marina.AddressState,
                    BestWindowId:     w.Id,
                    BasePricePerNight: w.BasePricePerNight,
                    InstantBook:      w.InstantBook,
                    CleaningFee:      w.CleaningFee,
                    MinNights:        w.MinNights,
                    MaxNights:        w.MaxNights,
                    DistanceMiles:    Math.Round(x.distance, 1)
                );
            })
            .ToList();
    }
}
