namespace MyMarina.Application.Search;

public sealed record SearchSlipsQuery(
    decimal Latitude,
    decimal Longitude,
    decimal RadiusMiles,
    // Transient: arrival+departure are required; Lease: both optional (fuzzy start)
    DateOnly? ArrivesAt,
    DateOnly? DepartsAt,
    decimal? VesselLength,
    decimal? VesselBeam,
    decimal? VesselDraft,
    string? SlipType,
    bool? HasElectric,
    bool? HasWater,
    string ListingKind,   // "Transient" (default) | "Lease"
    string? LeaseTerm,    // "Monthly" | "Seasonal" | "Annual" — only for Lease searches
    int Page,
    int PageSize,
    bool IncludeDemo
);

public sealed record GetPublicSlipDetailQuery(Guid SlipId, bool IncludeDemo);
