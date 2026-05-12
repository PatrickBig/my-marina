using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Search;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Search;

public class SearchSlipsAtMarinaQueryHandler(AppDbContext db)
    : IQueryHandler<SearchSlipsAtMarinaQuery, IReadOnlyList<SlipSearchResultDto>>
{
    public async Task<IReadOnlyList<SlipSearchResultDto>> HandleAsync(
        SearchSlipsAtMarinaQuery query, CancellationToken ct = default)
    {
        bool isLease = string.Equals(query.ListingKind, "Lease", StringComparison.OrdinalIgnoreCase);

        LeaseTerm? leaseTermFilter = null;
        if (isLease && !string.IsNullOrWhiteSpace(query.LeaseTerm)
            && Enum.TryParse<LeaseTerm>(query.LeaseTerm, ignoreCase: true, out var parsedTerm))
            leaseTermFilter = parsedTerm;

        // Verify marina exists and is visible
        var marina = await db.Marinas
            .Include(m => m.Tenant)
            .FirstOrDefaultAsync(m => m.Id == query.MarinaId, ct);

        if (marina is null || (!query.IncludeDemo && marina.Tenant.IsDemo))
            throw new KeyNotFoundException($"Marina {query.MarinaId} not found.");

        var baseSlipQuery = db.Slips
            .AsNoTracking()
            .Where(s => s.MarinaId == query.MarinaId);

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

        var eligible = await SlipAvailabilityFilter.GetEligibleSlipsAsync(db, baseSlipQuery, filters, ct);

        return eligible
            .OrderBy(e => e.Price)
            .Skip(query.Page * query.PageSize)
            .Take(query.PageSize)
            .Select(e => new SlipSearchResultDto(
                SlipId:           e.Slip.Id,
                SlipName:         e.Slip.Name,
                SlipType:         e.Slip.SlipType.ToString(),
                MaxLength:        e.Slip.MaxLength,
                MaxBeam:          e.Slip.MaxBeam,
                MaxDraft:         e.Slip.MaxDraft,
                HasElectric:      e.Slip.HasElectric,
                HasWater:         e.Slip.HasWater,
                Latitude:         e.Slip.Latitude ?? marina.Latitude ?? 0m,
                Longitude:        e.Slip.Longitude ?? marina.Longitude ?? 0m,
                MarinaId:         marina.Id,
                MarinaName:       marina.Name,
                MarinaCity:       marina.AddressCity,
                MarinaState:      marina.AddressState,
                BestWindowId:     e.BestWindowId,
                ListingKind:      e.ListingKind,
                RateKind:         e.RateKind,
                BasePricePerNight: e.Price,
                MinCharge:        e.MinCharge,
                LeaseTerm:        e.LeaseTerm,
                InstantBook:      e.InstantBook,
                CleaningFee:      e.CleaningFee,
                MinNights:        e.MinNights,
                MaxNights:        e.MaxNights,
                DistanceMiles:    0
            ))
            .ToList();
    }
}
