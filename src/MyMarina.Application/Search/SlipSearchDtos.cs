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
    double DistanceMilesFromCenter
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
    // Transient direct-booking rate (no window required)
    string? DefaultTransientRateKind,   // "Flat" | "PerFoot" | null
    decimal? DefaultTransientBaseRate,
    decimal? DefaultTransientMinCharge,
    bool TransientBookingAvailable,     // true when rate set + no conflicting assignment/reservation
    // Lease default rate
    string? DefaultLeaseRateKind,       // "Flat" | "PerFoot" | null
    decimal? DefaultLeaseBaseRate,
    string? DefaultLeaseTerm,           // "Monthly" | "Seasonal" | "Annual" | null
    bool LeaseInquiryAvailable,         // true when lease rate set
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
