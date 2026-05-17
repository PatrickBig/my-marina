namespace MyMarina.Application.Search;

// ─── Boater-facing public search queries ────────────────────────────────────

// Step 1: Bounding-box marina rollup query
public sealed record MarinaRollupSearchQuery(
    decimal North,
    decimal South,
    decimal East,
    decimal West,
    DateOnly? ArrivesAt,
    DateOnly? DepartsAt,
    decimal? VesselLength,
    decimal? VesselBeam,
    decimal? VesselDraft,
    string? SlipType,
    bool? HasElectric,
    bool? HasWater,
    string ListingKind,   // "Transient" (default) | "Lease"
    string? LeaseTerm,   // "Monthly" | "Seasonal" | "Annual" — only for Lease searches
    int Page,
    int PageSize,
    bool IncludeDemo
);

// Per-marina slip search (boater-facing step 2)
public sealed record SearchSlipsAtMarinaQuery(
    Guid MarinaId,
    DateOnly? ArrivesAt,
    DateOnly? DepartsAt,
    decimal? VesselLength,
    decimal? VesselBeam,
    decimal? VesselDraft,
    string? SlipType,
    bool? HasElectric,
    bool? HasWater,
    string ListingKind,   // "Transient" (default) | "Lease"
    string? LeaseTerm,
    int Page,
    int PageSize,
    bool IncludeDemo
);

public sealed record GetPublicSlipDetailQuery(Guid SlipId, bool IncludeDemo);
