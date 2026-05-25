namespace MyMarina.Application.Search;

public sealed record MarinaRollupResultDto(
    Guid MarinaId,
    string MarinaName,
    string? City,
    string? State,
    decimal Latitude,
    decimal Longitude,
    int AvailableCount,
    bool InstantBookAvailable,
    double DistanceMilesFromCenter,
    string? LogoUrl,
    string? BannerThumbnailUrl,
    bool HasPumpOut,
    bool HasElectric,
    bool IsAnyCovered
);

public sealed record SlipSearchResultDto(
    Guid SlipId,
    string SlipName,
    string SlipType,
    decimal MaxLength,
    decimal MaxBeam,
    decimal MaxDraft,
    bool HasElectric,
    bool HasWater,
    decimal Latitude,
    decimal Longitude,
    Guid MarinaId,
    string MarinaName,
    string? MarinaCity,
    string? MarinaState,
    Guid? BestWindowId,       // null = direct marina default rate (no AvailabilityWindow)
    string ListingKind,       // "Transient" | "Lease"
    string RateKind,          // "Flat" | "PerFoot"
    decimal BasePricePerNight, // rate amount; interpretation depends on ListingKind + RateKind
    decimal? MinCharge,
    string? LeaseTerm,        // "Monthly" | "Seasonal" | "Annual" — set for Lease results
    bool InstantBook,         // always false for lease results
    decimal? CleaningFee,
    int? MinNights,
    int? MaxNights,
    double DistanceMiles
);

public sealed record SlipDetailDto(
    Guid Id,
    string Name,
    string SlipType,
    decimal MaxLength,
    decimal MaxBeam,
    decimal MaxDraft,
    bool HasElectric,
    int? Electric,
    bool HasWater,
    decimal? Latitude,
    decimal? Longitude,
    string? AddressCity,
    string? AddressState,
    Guid MarinaId,
    string MarinaName,
    string? MarinaDescription,
    string? MarinaPhoneNumber,
    // Resolved rates — cached from the assigned pricing plan, null = listing kind not supported.
    decimal? ResolvedTransientBaseRate,
    bool TransientBookingAvailable,
    decimal? ResolvedLeaseBaseRate,
    bool LeaseInquiryAvailable,
    IReadOnlyList<PublicWindowSummaryDto> OpenWindows
);

public sealed record PublicWindowSummaryDto(
    Guid Id,
    string ListingKind,     // "Transient" | "Lease"
    string? LeaseTerm,      // "Monthly" | "Seasonal" | "Annual" — set for Lease windows
    string RateKind,        // "Flat" | "PerFoot"
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    bool InstantBook,
    int? MinNights,
    int? MaxNights,
    decimal BasePricePerNight,
    decimal? MinCharge,
    decimal? WeeklyDiscount,
    decimal? MonthlyDiscount,
    decimal? CleaningFee
);
