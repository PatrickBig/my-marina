using Microsoft.EntityFrameworkCore;
using MyMarina.Domain.Entities;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Search;

internal static class SlipAvailabilityFilter
{
    internal sealed record Filters(
        DateOnly? ArrivesAt,
        DateOnly? DepartsAt,
        decimal? VesselLength,
        decimal? VesselBeam,
        decimal? VesselDraft,
        string? SlipType,
        bool? HasElectric,
        bool? HasWater,
        bool IsLease,
        LeaseTerm? LeaseTermFilter,
        bool IncludeDemo
    );

    // Returns all eligible SlipSearchResultDto-ready tuples within a pre-filtered slip set.
    // Callers supply the base slip query already scoped to their geographic constraint.
    internal static async Task<List<EligibleSlip>> GetEligibleSlipsAsync(
        AppDbContext db,
        IQueryable<Slip> baseSlipQuery,
        Filters f,
        CancellationToken ct)
    {
        SlipType slipTypeFilter = default;
        bool hasSlipTypeFilter = !string.IsNullOrWhiteSpace(f.SlipType)
            && Enum.TryParse(f.SlipType, ignoreCase: true, out slipTypeFilter);

        var today = DateOnly.FromDateTime(DateTime.Today);

        // Apply common dimension + amenity filters
        var slipQ = baseSlipQuery
            .Where(s => s.Status == SlipStatus.Active
                     && (f.VesselLength == null || s.MaxLength >= f.VesselLength)
                     && (f.VesselBeam   == null || s.MaxBeam   >= f.VesselBeam)
                     && (f.VesselDraft  == null || s.MaxDraft  >= f.VesselDraft)
                     && (!hasSlipTypeFilter || s.SlipType == slipTypeFilter)
                     && (f.HasElectric == null || s.HasElectric == f.HasElectric)
                     && (f.HasWater    == null || s.HasWater    == f.HasWater));

        return f.IsLease
            ? await GetLeaseEligibleAsync(db, slipQ, f, today, ct)
            : await GetTransientEligibleAsync(db, slipQ, f, today, ct);
    }

    private static async Task<List<EligibleSlip>> GetTransientEligibleAsync(
        AppDbContext db,
        IQueryable<Slip> slipQ,
        Filters f,
        DateOnly today,
        CancellationToken ct)
    {
        var arrivesAt = (f.ArrivesAt ?? today).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var departsAt = (f.DepartsAt ?? f.ArrivesAt?.AddDays(1) ?? today.AddDays(1))
                            .ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var arrivesDateOnly = DateOnly.FromDateTime(arrivesAt);
        var departsDateOnly = DateOnly.FromDateTime(departsAt);

        // Path A: explicit transient AvailabilityWindows
        var windowSlipIds = await db.AvailabilityWindows
            .Where(w => w.Status == AvailabilityWindowStatus.Open
                     && w.ListingKind == ListingKind.Transient
                     && w.StartsAt <= arrivesAt
                     && w.EndsAt   >= departsAt)
            .Select(w => w.SlipId)
            .Distinct()
            .ToListAsync(ct);

        var results = new List<EligibleSlip>();

        if (windowSlipIds.Count > 0)
        {
            var windowSlips = await slipQ
                .Where(s => windowSlipIds.Contains(s.Id))
                .ToListAsync(ct);

            if (windowSlips.Count > 0)
            {
                var slipIdList = windowSlips.Select(s => s.Id).ToList();
                var windows = await db.AvailabilityWindows
                    .Where(w => slipIdList.Contains(w.SlipId)
                             && w.Status == AvailabilityWindowStatus.Open
                             && w.ListingKind == ListingKind.Transient
                             && w.StartsAt <= arrivesAt
                             && w.EndsAt   >= departsAt)
                    .ToListAsync(ct);

                var bestWindows = windows
                    .GroupBy(w => w.SlipId)
                    .ToDictionary(g => g.Key, g => g.OrderBy(w => w.BasePricePerNight).First());

                foreach (var s in windowSlips.Where(s => bestWindows.ContainsKey(s.Id)))
                {
                    var w = bestWindows[s.Id];
                    results.Add(new EligibleSlip(
                        Slip: s,
                        BestWindowId: w.Id,
                        ListingKind: "Transient",
                        RateKind: w.RateKind.ToString(),
                        Price: w.BasePricePerNight,
                        MinCharge: w.MinCharge,
                        LeaseTerm: null,
                        InstantBook: w.InstantBook,
                        CleaningFee: w.CleaningFee,
                        MinNights: w.MinNights,
                        MaxNights: w.MaxNights
                    ));
                }
            }
        }

        // Path B: direct resolved transient rate (no window needed)
        var pathAIds = results.Select(r => r.Slip.Id).ToList();

        var directSlips = await slipQ
            .Where(s => s.ResolvedTransientBaseRate != null
                     && !pathAIds.Contains(s.Id)
                     && !db.SlipAssignments.Any(a =>
                            a.SlipId == s.Id &&
                            a.StartDate <= departsDateOnly &&
                            (a.EndDate == null || a.EndDate >= arrivesDateOnly))
                     && !db.Reservations.Any(r =>
                            r.SlipId == s.Id &&
                            r.ArrivesAt < departsAt &&
                            r.DepartsAt > arrivesAt &&
                            r.Status != ReservationStatus.Declined &&
                            r.Status != ReservationStatus.Cancelled))
            .ToListAsync(ct);

        foreach (var s in directSlips)
        {
            results.Add(new EligibleSlip(
                Slip: s,
                BestWindowId: null,
                ListingKind: "Transient",
                RateKind: RateKind.Flat.ToString(),
                Price: s.ResolvedTransientBaseRate!.Value,
                MinCharge: null,
                LeaseTerm: null,
                InstantBook: true,
                CleaningFee: null,
                MinNights: 1,
                MaxNights: null
            ));
        }

        return results;
    }

    private static async Task<List<EligibleSlip>> GetLeaseEligibleAsync(
        AppDbContext db,
        IQueryable<Slip> slipQ,
        Filters f,
        DateOnly today,
        CancellationToken ct)
    {
        DateTime? desiredStart = f.ArrivesAt.HasValue
            ? f.ArrivesAt.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
            : null;

        // Path A: explicit lease AvailabilityWindows
        var leaseWindowQ = db.AvailabilityWindows
            .Where(w => w.Status == AvailabilityWindowStatus.Open
                     && w.ListingKind == ListingKind.Lease
                     && (f.LeaseTermFilter == null || w.LeaseTerm == f.LeaseTermFilter));

        if (desiredStart.HasValue)
            leaseWindowQ = leaseWindowQ.Where(w => w.StartsAt <= desiredStart && w.EndsAt >= desiredStart);

        var windowSlipIds = await leaseWindowQ.Select(w => w.SlipId).Distinct().ToListAsync(ct);

        var results = new List<EligibleSlip>();

        if (windowSlipIds.Count > 0)
        {
            var windowSlips = await slipQ
                .Where(s => windowSlipIds.Contains(s.Id)
                         && !db.SlipAssignments.Any(a =>
                                a.SlipId == s.Id &&
                                (a.EndDate == null || a.EndDate >= today)))
                .ToListAsync(ct);

            if (windowSlips.Count > 0)
            {
                var slipIdList = windowSlips.Select(s => s.Id).ToList();
                var windows = await db.AvailabilityWindows
                    .Where(w => slipIdList.Contains(w.SlipId)
                             && w.Status == AvailabilityWindowStatus.Open
                             && w.ListingKind == ListingKind.Lease
                             && (f.LeaseTermFilter == null || w.LeaseTerm == f.LeaseTermFilter))
                    .ToListAsync(ct);

                var bestWindows = windows
                    .GroupBy(w => w.SlipId)
                    .ToDictionary(g => g.Key, g => g.OrderBy(w => w.BasePricePerNight).First());

                foreach (var s in windowSlips.Where(s => bestWindows.ContainsKey(s.Id)))
                {
                    var w = bestWindows[s.Id];
                    results.Add(new EligibleSlip(
                        Slip: s,
                        BestWindowId: w.Id,
                        ListingKind: "Lease",
                        RateKind: w.RateKind.ToString(),
                        Price: w.BasePricePerNight,
                        MinCharge: w.MinCharge,
                        LeaseTerm: w.LeaseTerm?.ToString(),
                        InstantBook: false,
                        CleaningFee: null,
                        MinNights: null,
                        MaxNights: null
                    ));
                }
            }
        }

        // Path B: direct resolved lease rate
        var pathAIds = results.Select(r => r.Slip.Id).ToList();

        var directSlips = await slipQ
            .Where(s => s.ResolvedLeaseBaseRate != null
                     && !pathAIds.Contains(s.Id)
                     && !db.SlipAssignments.Any(a => a.SlipId == s.Id && (a.EndDate == null || a.EndDate >= today)))
            .ToListAsync(ct);

        foreach (var s in directSlips)
        {
            results.Add(new EligibleSlip(
                Slip: s,
                BestWindowId: null,
                ListingKind: "Lease",
                RateKind: RateKind.Flat.ToString(),
                Price: s.ResolvedLeaseBaseRate!.Value,
                MinCharge: null,
                LeaseTerm: null,
                InstantBook: false,
                CleaningFee: null,
                MinNights: null,
                MaxNights: null
            ));
        }

        return results;
    }

    internal sealed record EligibleSlip(
        Slip Slip,
        Guid? BestWindowId,
        string ListingKind,
        string RateKind,
        decimal Price,
        decimal? MinCharge,
        string? LeaseTerm,
        bool InstantBook,
        decimal? CleaningFee,
        int? MinNights,
        int? MaxNights
    );

}
