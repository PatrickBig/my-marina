using MyMarina.Application.AvailabilityWindows;
using MyMarina.Domain.Entities;

namespace MyMarina.Infrastructure.AvailabilityWindows;

internal static class AvailabilityWindowHelper
{
    internal static bool DateRangesOverlap(DateTimeOffset s1, DateTimeOffset e1, DateTimeOffset s2, DateTimeOffset e2)
        => s1 < e2 && s2 < e1;

    internal static AvailabilityWindowDto ToDto(AvailabilityWindow w) => new(
        Id:                      w.Id,
        SlipId:                  w.SlipId,
        SlipName:                w.Slip?.Name ?? string.Empty,
        ListedByKind:            w.ListedByKind.ToString(),
        ListedByMarinaId:        w.ListedByMarinaId,
        ListedByBillingAccountId: w.ListedByBillingAccountId,
        RelatedAssignmentId:     w.RelatedAssignmentId,
        StartsAt:                w.StartsAt,
        EndsAt:                  w.EndsAt,
        InstantBook:             w.InstantBook,
        MinNights:               w.MinNights,
        MaxNights:               w.MaxNights,
        BasePricePerNight:       w.BasePricePerNight,
        WeeklyDiscount:          w.WeeklyDiscount,
        MonthlyDiscount:         w.MonthlyDiscount,
        CleaningFee:             w.CleaningFee,
        RevenueSplit:            w.RevenueSplit.Select(e => new RevenueSplitEntryDto(e.PayeeKind, e.PayeeId, e.Percent)).ToList(),
        Status:                  w.Status.ToString(),
        CreatedAt:               w.CreatedAt
    );
}
