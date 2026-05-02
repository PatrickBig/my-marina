namespace MyMarina.Application.Search;

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
    Guid BestWindowId,
    decimal BasePricePerNight,
    bool InstantBook,
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
    IReadOnlyList<PublicWindowSummaryDto> OpenWindows
);

public sealed record PublicWindowSummaryDto(
    Guid Id,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    bool InstantBook,
    int? MinNights,
    int? MaxNights,
    decimal BasePricePerNight,
    decimal? WeeklyDiscount,
    decimal? MonthlyDiscount,
    decimal? CleaningFee
);
