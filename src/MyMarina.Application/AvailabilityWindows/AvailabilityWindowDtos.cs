namespace MyMarina.Application.AvailabilityWindows;

public sealed record RevenueSplitEntryDto(
    string PayeeKind,
    Guid? PayeeId,
    decimal Percent
);

public sealed record AvailabilityWindowDto(
    Guid Id,
    Guid SlipId,
    string SlipName,
    string ListedByKind,
    Guid? ListedByMarinaId,
    Guid? ListedByBillingAccountId,
    Guid? RelatedAssignmentId,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    bool InstantBook,
    int? MinNights,
    int? MaxNights,
    decimal BasePricePerNight,
    decimal? WeeklyDiscount,
    decimal? MonthlyDiscount,
    decimal? CleaningFee,
    IReadOnlyList<RevenueSplitEntryDto> RevenueSplit,
    string Status,
    DateTimeOffset CreatedAt
);
