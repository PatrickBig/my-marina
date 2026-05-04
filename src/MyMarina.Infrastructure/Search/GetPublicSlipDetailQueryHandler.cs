using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Search;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Search;

public class GetPublicSlipDetailQueryHandler(AppDbContext db)
    : IQueryHandler<GetPublicSlipDetailQuery, SlipDetailDto>
{
    public async Task<SlipDetailDto> HandleAsync(GetPublicSlipDetailQuery query, CancellationToken ct = default)
    {
        var slip = await db.Slips
            .FirstOrDefaultAsync(s => s.Id == query.SlipId && s.Status == SlipStatus.Active, ct)
            ?? throw new KeyNotFoundException($"Slip {query.SlipId} not found.");

        var marina = await db.Marinas
            .Include(m => m.Tenant)
            .FirstOrDefaultAsync(m => m.Id == slip.MarinaId, ct)
            ?? throw new KeyNotFoundException();

        if (marina.Tenant.IsDemo && !query.IncludeDemo)
            throw new KeyNotFoundException($"Slip {query.SlipId} not found.");

        var openWindows = await db.AvailabilityWindows
            .Where(w => w.SlipId == slip.Id && w.Status == AvailabilityWindowStatus.Open)
            .OrderBy(w => w.StartsAt)
            .ToListAsync(ct);

        var effectiveCity  = slip.AddressCity  ?? marina.AddressCity;
        var effectiveState = slip.AddressState ?? marina.AddressState;

        return new SlipDetailDto(
            Id:               slip.Id,
            Name:             slip.Name,
            SlipType:         slip.SlipType.ToString(),
            MaxLength:        slip.MaxLength,
            MaxBeam:          slip.MaxBeam,
            MaxDraft:         slip.MaxDraft,
            HasElectric:      slip.HasElectric,
            Electric:         slip.Electric.HasValue ? (int)slip.Electric.Value : null,
            HasWater:         slip.HasWater,
            Latitude:         slip.Latitude ?? marina.Latitude,
            Longitude:        slip.Longitude ?? marina.Longitude,
            AddressCity:      effectiveCity,
            AddressState:     effectiveState,
            MarinaId:         marina.Id,
            MarinaName:       marina.Name,
            MarinaDescription: marina.Description,
            MarinaPhoneNumber: marina.PhoneNumber,
            DefaultTransientRateKind:  slip.DefaultTransientRateKind?.ToString(),
            DefaultTransientBaseRate:  slip.DefaultTransientBaseRate,
            DefaultTransientMinCharge: slip.DefaultTransientMinCharge,
            TransientBookingAvailable: slip.DefaultTransientRateKind.HasValue,
            DefaultLeaseRateKind:      slip.DefaultLeaseRateKind?.ToString(),
            DefaultLeaseBaseRate:      slip.DefaultLeaseBaseRate,
            DefaultLeaseTerm:          slip.DefaultLeaseTerm?.ToString(),
            LeaseInquiryAvailable:     slip.DefaultLeaseRateKind.HasValue,
            OpenWindows: openWindows.Select(w => new PublicWindowSummaryDto(
                Id:               w.Id,
                ListingKind:      w.ListingKind.ToString(),
                LeaseTerm:        w.LeaseTerm?.ToString(),
                RateKind:         w.RateKind.ToString(),
                StartsAt:         w.StartsAt,
                EndsAt:           w.EndsAt,
                InstantBook:      w.InstantBook,
                MinNights:        w.MinNights,
                MaxNights:        w.MaxNights,
                BasePricePerNight: w.BasePricePerNight,
                MinCharge:        w.MinCharge,
                WeeklyDiscount:   w.WeeklyDiscount,
                MonthlyDiscount:  w.MonthlyDiscount,
                CleaningFee:      w.CleaningFee
            )).ToList()
        );
    }
}
