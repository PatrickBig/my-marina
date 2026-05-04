using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Search;
using MyMarina.Domain.Entities;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Search;

public class SearchSlipsQueryHandler(AppDbContext db)
    : IQueryHandler<SearchSlipsQuery, IReadOnlyList<SlipSearchResultDto>>
{
    public async Task<IReadOnlyList<SlipSearchResultDto>> HandleAsync(SearchSlipsQuery query, CancellationToken ct = default)
    {
        var (minLat, maxLat, minLon, maxLon) = GeoHelper.BoundingBox(query.Latitude, query.Longitude, query.RadiusMiles);

        bool isLeaseSearch = string.Equals(query.ListingKind, "Lease", StringComparison.OrdinalIgnoreCase);

        SlipType slipTypeFilter = default;
        bool hasSlipTypeFilter = !string.IsNullOrWhiteSpace(query.SlipType)
            && Enum.TryParse(query.SlipType, ignoreCase: true, out slipTypeFilter);

        return isLeaseSearch
            ? await SearchLease(query, minLat, maxLat, minLon, maxLon, hasSlipTypeFilter, slipTypeFilter, ct)
            : await SearchTransient(query, minLat, maxLat, minLon, maxLon, hasSlipTypeFilter, slipTypeFilter, ct);
    }

    // ─── Lease search ────────────────────────────────────────────────────────────

    private async Task<IReadOnlyList<SlipSearchResultDto>> SearchLease(
        SearchSlipsQuery query,
        decimal minLat, decimal maxLat, decimal minLon, decimal maxLon,
        bool hasSlipTypeFilter, SlipType slipTypeFilter,
        CancellationToken ct)
    {
        LeaseTerm? leaseTermFilter = null;
        if (!string.IsNullOrWhiteSpace(query.LeaseTerm) &&
            Enum.TryParse<LeaseTerm>(query.LeaseTerm, ignoreCase: true, out var parsedTerm))
            leaseTermFilter = parsedTerm;

        var today = DateOnly.FromDateTime(DateTime.Today);

        // Path A: explicit Lease AvailabilityWindows
        var leaseWindowQuery = db.AvailabilityWindows
            .Where(w => w.Status == AvailabilityWindowStatus.Open
                     && w.ListingKind == ListingKind.Lease
                     && (leaseTermFilter == null || w.LeaseTerm == leaseTermFilter));

        if (query.ArrivesAt.HasValue)
        {
            var desiredStart = query.ArrivesAt.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            leaseWindowQuery = leaseWindowQuery.Where(w => w.StartsAt <= desiredStart && w.EndsAt >= desiredStart);
        }

        var eligibleLeaseWindowSlipIds = await leaseWindowQuery
            .Select(w => w.SlipId)
            .Distinct()
            .ToListAsync(ct);

        var pathAResults = new List<SlipSearchResultDto>();

        if (eligibleLeaseWindowSlipIds.Count > 0)
        {
            var windowSlips = await db.Slips
                .Where(s => eligibleLeaseWindowSlipIds.Contains(s.Id)
                         && s.Status == SlipStatus.Active
                         && !db.SlipAssignments.Any(a =>
                                a.SlipId == s.Id &&
                                (a.EndDate == null || a.EndDate >= today))
                         && (query.VesselLength == null || s.MaxLength >= query.VesselLength)
                         && (query.VesselBeam   == null || s.MaxBeam   >= query.VesselBeam)
                         && (query.VesselDraft  == null || s.MaxDraft  >= query.VesselDraft)
                         && (!hasSlipTypeFilter || s.SlipType == slipTypeFilter)
                         && (query.HasElectric == null || s.HasElectric == query.HasElectric)
                         && (query.HasWater    == null || s.HasWater    == query.HasWater))
                .ToListAsync(ct);

            if (windowSlips.Count > 0)
            {
                var mIds = windowSlips.Select(s => s.MarinaId).Distinct().ToList();
                var marinas = await db.Marinas.Include(m => m.Tenant)
                    .Where(m => mIds.Contains(m.Id))
                    .ToDictionaryAsync(m => m.Id, ct);

                var windows = await db.AvailabilityWindows
                    .Where(w => eligibleLeaseWindowSlipIds.Contains(w.SlipId)
                             && w.Status == AvailabilityWindowStatus.Open
                             && w.ListingKind == ListingKind.Lease
                             && (leaseTermFilter == null || w.LeaseTerm == leaseTermFilter))
                    .ToListAsync(ct);

                var bestLeaseWindows = windows
                    .GroupBy(w => w.SlipId)
                    .ToDictionary(g => g.Key, g => g.OrderBy(w => w.BasePricePerNight).First());

                pathAResults = windowSlips
                    .Where(s => marinas.TryGetValue(s.MarinaId, out var m) && (query.IncludeDemo || !m.Tenant.IsDemo)
                             && bestLeaseWindows.ContainsKey(s.Id))
                    .Select(s => {
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
                        lat: x.lat!.Value, lon: x.lon!.Value,
                        distance: GeoHelper.HaversineDistanceMiles(
                            (double)query.Latitude, (double)query.Longitude,
                            (double)x.lat!.Value, (double)x.lon!.Value)
                    ))
                    .Where(x => x.distance <= (double)query.RadiusMiles)
                    .Select(x => {
                        var w = bestLeaseWindows[x.slip.Id];
                        return new SlipSearchResultDto(
                            SlipId: x.slip.Id, SlipName: x.slip.Name,
                            SlipType: x.slip.SlipType.ToString(),
                            MaxLength: x.slip.MaxLength, MaxBeam: x.slip.MaxBeam, MaxDraft: x.slip.MaxDraft,
                            HasElectric: x.slip.HasElectric, HasWater: x.slip.HasWater,
                            Latitude: x.lat, Longitude: x.lon,
                            MarinaId: x.marina.Id, MarinaName: x.marina.Name,
                            MarinaCity: x.marina.AddressCity, MarinaState: x.marina.AddressState,
                            BestWindowId: w.Id,
                            ListingKind: "Lease",
                            RateKind: w.RateKind.ToString(),
                            BasePricePerNight: w.BasePricePerNight,
                            MinCharge: w.MinCharge,
                            LeaseTerm: w.LeaseTerm?.ToString(),
                            InstantBook: false,
                            CleaningFee: null, MinNights: null, MaxNights: null,
                            DistanceMiles: Math.Round(x.distance, 1)
                        );
                    })
                    .ToList();
            }
        }

        // Path B: slips with DefaultLeaseBaseRate set (no explicit window)
        var alreadyFoundSlipIds = pathAResults.Select(r => r.SlipId).ToList();

        var directLeaseSlips = await db.Slips
            .Where(s => s.Status == SlipStatus.Active
                     && s.DefaultLeaseRateKind != null
                     && !alreadyFoundSlipIds.Contains(s.Id)
                     && !db.SlipAssignments.Any(a => a.SlipId == s.Id && (a.EndDate == null || a.EndDate >= today))
                     && (leaseTermFilter == null || s.DefaultLeaseTerm == leaseTermFilter)
                     && (query.VesselLength == null || s.MaxLength >= query.VesselLength)
                     && (query.VesselBeam   == null || s.MaxBeam   >= query.VesselBeam)
                     && (query.VesselDraft  == null || s.MaxDraft  >= query.VesselDraft)
                     && (!hasSlipTypeFilter || s.SlipType == slipTypeFilter)
                     && (query.HasElectric == null || s.HasElectric == query.HasElectric)
                     && (query.HasWater    == null || s.HasWater    == query.HasWater))
            .ToListAsync(ct);

        var pathBResults = new List<SlipSearchResultDto>();

        if (directLeaseSlips.Count > 0)
        {
            var directMarinaIds = directLeaseSlips.Select(s => s.MarinaId).Distinct().ToList();
            var directMarinas = await db.Marinas.Include(m => m.Tenant)
                .Where(m => directMarinaIds.Contains(m.Id))
                .ToDictionaryAsync(m => m.Id, ct);

            pathBResults = directLeaseSlips
                .Where(s => directMarinas.TryGetValue(s.MarinaId, out var m) && (query.IncludeDemo || !m.Tenant.IsDemo))
                .Select(s => {
                    var m = directMarinas[s.MarinaId];
                    var lat = s.Latitude ?? m.Latitude;
                    var lon = s.Longitude ?? m.Longitude;
                    return (slip: s, marina: m, lat, lon);
                })
                .Where(x => x.lat != null && x.lon != null
                         && x.lat >= minLat && x.lat <= maxLat
                         && x.lon >= minLon && x.lon <= maxLon)
                .Select(x => (
                    x.slip, x.marina,
                    lat: x.lat!.Value, lon: x.lon!.Value,
                    distance: GeoHelper.HaversineDistanceMiles(
                        (double)query.Latitude, (double)query.Longitude,
                        (double)x.lat!.Value, (double)x.lon!.Value)
                ))
                .Where(x => x.distance <= (double)query.RadiusMiles)
                .Select(x => new SlipSearchResultDto(
                    SlipId: x.slip.Id, SlipName: x.slip.Name,
                    SlipType: x.slip.SlipType.ToString(),
                    MaxLength: x.slip.MaxLength, MaxBeam: x.slip.MaxBeam, MaxDraft: x.slip.MaxDraft,
                    HasElectric: x.slip.HasElectric, HasWater: x.slip.HasWater,
                    Latitude: x.lat, Longitude: x.lon,
                    MarinaId: x.marina.Id, MarinaName: x.marina.Name,
                    MarinaCity: x.marina.AddressCity, MarinaState: x.marina.AddressState,
                    BestWindowId: null,
                    ListingKind: "Lease",
                    RateKind: x.slip.DefaultLeaseRateKind!.Value.ToString(),
                    BasePricePerNight: x.slip.DefaultLeaseBaseRate!.Value,
                    MinCharge: null,
                    LeaseTerm: x.slip.DefaultLeaseTerm?.ToString(),
                    InstantBook: false,
                    CleaningFee: null, MinNights: null, MaxNights: null,
                    DistanceMiles: Math.Round(x.distance, 1)
                ))
                .ToList();
        }

        return [.. pathAResults.Concat(pathBResults)
            .OrderBy(r => r.DistanceMiles)
            .ThenBy(r => r.BasePricePerNight)
            .Skip(query.Page * query.PageSize)
            .Take(query.PageSize)];
    }

    // ─── Transient search ────────────────────────────────────────────────────────

    private async Task<IReadOnlyList<SlipSearchResultDto>> SearchTransient(
        SearchSlipsQuery query,
        decimal minLat, decimal maxLat, decimal minLon, decimal maxLon,
        bool hasSlipTypeFilter, SlipType slipTypeFilter,
        CancellationToken ct)
    {
        var arrivesAt = (query.ArrivesAt ?? DateOnly.FromDateTime(DateTime.Today))
            .ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var departsAt = (query.DepartsAt ?? query.ArrivesAt?.AddDays(1) ?? DateOnly.FromDateTime(DateTime.Today.AddDays(1)))
            .ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);

        // ─── Path A: explicit transient AvailabilityWindows ──────────────────────

        var eligibleSlipIds = await db.AvailabilityWindows
            .Where(w => w.Status == AvailabilityWindowStatus.Open
                     && w.ListingKind == ListingKind.Transient
                     && w.StartsAt <= arrivesAt
                     && w.EndsAt   >= departsAt)
            .Select(w => w.SlipId)
            .Distinct()
            .ToListAsync(ct);

        List<(Slip slip, Marina marina, decimal lat, decimal lon, double distance)> windowCandidates = [];

        if (eligibleSlipIds.Count > 0)
        {
            var windowSlips = await db.Slips
                .Where(s => eligibleSlipIds.Contains(s.Id)
                         && s.Status == SlipStatus.Active
                         && (query.VesselLength == null || s.MaxLength >= query.VesselLength)
                         && (query.VesselBeam   == null || s.MaxBeam   >= query.VesselBeam)
                         && (query.VesselDraft  == null || s.MaxDraft  >= query.VesselDraft)
                         && (!hasSlipTypeFilter || s.SlipType == slipTypeFilter)
                         && (query.HasElectric == null || s.HasElectric == query.HasElectric)
                         && (query.HasWater    == null || s.HasWater    == query.HasWater))
                .ToListAsync(ct);

            if (windowSlips.Count > 0)
            {
                var mIds = windowSlips.Select(s => s.MarinaId).Distinct().ToList();
                var marinas = await db.Marinas.Include(m => m.Tenant)
                    .Where(m => mIds.Contains(m.Id))
                    .ToDictionaryAsync(m => m.Id, ct);

                windowCandidates = windowSlips
                    .Where(s => marinas.TryGetValue(s.MarinaId, out var m) && (query.IncludeDemo || !m.Tenant.IsDemo))
                    .Select(s => {
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
                        lat: x.lat!.Value, lon: x.lon!.Value,
                        distance: GeoHelper.HaversineDistanceMiles(
                            (double)query.Latitude, (double)query.Longitude,
                            (double)x.lat!.Value, (double)x.lon!.Value)
                    ))
                    .Where(x => x.distance <= (double)query.RadiusMiles)
                    .ToList();
            }
        }

        var windowCandidateSlipIds = windowCandidates.Select(x => x.slip.Id).ToList();
        var allWindows = windowCandidateSlipIds.Count > 0
            ? await db.AvailabilityWindows
                .Where(w => windowCandidateSlipIds.Contains(w.SlipId)
                         && w.Status == AvailabilityWindowStatus.Open
                         && w.ListingKind == ListingKind.Transient
                         && w.StartsAt <= arrivesAt
                         && w.EndsAt   >= departsAt)
                .ToListAsync(ct)
            : [];

        var bestWindows = allWindows
            .GroupBy(w => w.SlipId)
            .ToDictionary(g => g.Key, g => g.OrderBy(w => w.BasePricePerNight).First());

        var pathAResults = windowCandidates
            .Where(x => bestWindows.ContainsKey(x.slip.Id))
            .Select(x => {
                var w = bestWindows[x.slip.Id];
                return new SlipSearchResultDto(
                    SlipId: x.slip.Id, SlipName: x.slip.Name,
                    SlipType: x.slip.SlipType.ToString(),
                    MaxLength: x.slip.MaxLength, MaxBeam: x.slip.MaxBeam, MaxDraft: x.slip.MaxDraft,
                    HasElectric: x.slip.HasElectric, HasWater: x.slip.HasWater,
                    Latitude: x.lat, Longitude: x.lon,
                    MarinaId: x.marina.Id, MarinaName: x.marina.Name,
                    MarinaCity: x.marina.AddressCity, MarinaState: x.marina.AddressState,
                    BestWindowId: w.Id,
                    ListingKind: "Transient",
                    RateKind: w.RateKind.ToString(),
                    BasePricePerNight: w.BasePricePerNight,
                    MinCharge: w.MinCharge,
                    LeaseTerm: null,
                    InstantBook: w.InstantBook,
                    CleaningFee: w.CleaningFee,
                    MinNights: w.MinNights,
                    MaxNights: w.MaxNights,
                    DistanceMiles: Math.Round(x.distance, 1)
                );
            })
            .ToList();

        // ─── Path B: active slips with DefaultTransientBaseRate, no window needed ─

        var alreadyFoundSlipIds = pathAResults.Select(r => r.SlipId).ToList();

        var arrivesDateOnly = DateOnly.FromDateTime(arrivesAt);
        var departsDateOnly = DateOnly.FromDateTime(departsAt);

        var directSlips = await db.Slips
            .Where(s => s.Status == SlipStatus.Active
                     && s.DefaultTransientRateKind != null
                     && !alreadyFoundSlipIds.Contains(s.Id)
                     && !db.SlipAssignments.Any(a =>
                            a.SlipId == s.Id &&
                            a.StartDate <= departsDateOnly &&
                            (a.EndDate == null || a.EndDate >= arrivesDateOnly))
                     && !db.Reservations.Any(r =>
                            r.SlipId == s.Id &&
                            r.ArrivesAt < departsAt &&
                            r.DepartsAt > arrivesAt &&
                            r.Status != ReservationStatus.Declined &&
                            r.Status != ReservationStatus.Cancelled)
                     && (query.VesselLength == null || s.MaxLength >= query.VesselLength)
                     && (query.VesselBeam   == null || s.MaxBeam   >= query.VesselBeam)
                     && (query.VesselDraft  == null || s.MaxDraft  >= query.VesselDraft)
                     && (!hasSlipTypeFilter || s.SlipType == slipTypeFilter)
                     && (query.HasElectric == null || s.HasElectric == query.HasElectric)
                     && (query.HasWater    == null || s.HasWater    == query.HasWater))
            .ToListAsync(ct);

        var pathBResults = new List<SlipSearchResultDto>();

        if (directSlips.Count > 0)
        {
            var directMarinaIds = directSlips.Select(s => s.MarinaId).Distinct().ToList();
            var directMarinas = await db.Marinas.Include(m => m.Tenant)
                .Where(m => directMarinaIds.Contains(m.Id))
                .ToDictionaryAsync(m => m.Id, ct);

            pathBResults = directSlips
                .Where(s => directMarinas.TryGetValue(s.MarinaId, out var m) && (query.IncludeDemo || !m.Tenant.IsDemo))
                .Select(s => {
                    var m = directMarinas[s.MarinaId];
                    var lat = s.Latitude ?? m.Latitude;
                    var lon = s.Longitude ?? m.Longitude;
                    return (slip: s, marina: m, lat, lon);
                })
                .Where(x => x.lat != null && x.lon != null
                         && x.lat >= minLat && x.lat <= maxLat
                         && x.lon >= minLon && x.lon <= maxLon)
                .Select(x => (
                    x.slip, x.marina,
                    lat: x.lat!.Value, lon: x.lon!.Value,
                    distance: GeoHelper.HaversineDistanceMiles(
                        (double)query.Latitude, (double)query.Longitude,
                        (double)x.lat!.Value, (double)x.lon!.Value)
                ))
                .Where(x => x.distance <= (double)query.RadiusMiles)
                .Select(x => new SlipSearchResultDto(
                    SlipId: x.slip.Id, SlipName: x.slip.Name,
                    SlipType: x.slip.SlipType.ToString(),
                    MaxLength: x.slip.MaxLength, MaxBeam: x.slip.MaxBeam, MaxDraft: x.slip.MaxDraft,
                    HasElectric: x.slip.HasElectric, HasWater: x.slip.HasWater,
                    Latitude: x.lat, Longitude: x.lon,
                    MarinaId: x.marina.Id, MarinaName: x.marina.Name,
                    MarinaCity: x.marina.AddressCity, MarinaState: x.marina.AddressState,
                    BestWindowId: null,
                    ListingKind: "Transient",
                    RateKind: x.slip.DefaultTransientRateKind!.Value.ToString(),
                    BasePricePerNight: x.slip.DefaultTransientBaseRate!.Value,
                    MinCharge: x.slip.DefaultTransientMinCharge,
                    LeaseTerm: null,
                    InstantBook: true,
                    CleaningFee: null, MinNights: 1, MaxNights: null,
                    DistanceMiles: Math.Round(x.distance, 1)
                ))
                .ToList();
        }

        return [.. pathAResults.Concat(pathBResults)
            .OrderBy(r => r.DistanceMiles)
            .ThenBy(r => r.BasePricePerNight)
            .Skip(query.Page * query.PageSize)
            .Take(query.PageSize)];
    }
}
