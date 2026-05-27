using MyMarina.Domain.Enums;

namespace MyMarina.Application.Marinas;

public sealed record TenantDto(
    Guid Id,
    string Name,
    string Slug,
    string SubscriptionTier,
    bool IsActive,
    DateTimeOffset CreatedAt
);

public sealed record MarinaDto(
    Guid Id,
    Guid TenantId,
    string Name,
    string Slug,
    string? AddressStreet,
    string? AddressCity,
    string? AddressState,
    string? AddressZip,
    string? AddressCountry,
    decimal? Latitude,
    decimal? Longitude,
    string? PhoneNumber,
    string? Email,
    string? Website,
    string? Description,
    string TimeZoneId,
    string MarinaType,
    bool IsListed,
    bool IsSetupComplete,
    int SetupStep,
    DateTimeOffset CreatedAt,
    string? LogoUrl = null,
    string? BannerThumbnailUrl = null
);

public sealed record DockDto(
    Guid Id,
    Guid MarinaId,
    string Name,
    string? Description,
    int SortOrder,
    DateTimeOffset CreatedAt
);

public sealed record SlipDto(
    Guid Id,
    Guid MarinaId,
    Guid? DockId,
    string Name,
    string SlipType,
    decimal MaxLength,
    decimal MaxBeam,
    decimal MaxDraft,
    bool HasElectric,
    int? Electric,
    bool HasWater,
    bool HasPumpOut,
    bool IsCovered,
    bool IsIndoor,
    IReadOnlyList<string> Amenities,
    string Status,
    // Resolved rates — cached from the assigned pricing plan, null = listing kind not supported.
    decimal? ResolvedTransientBaseRate,
    decimal? ResolvedLeaseBaseRate,
    Guid? PricingPlanId,
    string? Notes,
    DateTimeOffset CreatedAt
);

// Returned from CreateMarinaAccountCommand — includes a fresh JWT so the user is
// immediately authorized for their new marina dashboard.
public sealed record MarinaSignupResponse(
    TenantDto Tenant,
    MarinaDto Marina,
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt
);

// Returned from SearchMarinasQuery — minimal projection for host marina lookup.
public sealed record MarinaSummaryDto(
    Guid Id,
    string Name,
    string? AddressCity,
    string? AddressState,
    string MarinaType,
    string? LogoUrl = null,
    string? BannerThumbnailUrl = null
);

// Returned from GetMarinaCompositionQuery — slip count breakdown by assignment type.
public sealed record MarinaCompositionDto(
    int Total,
    int Annual,
    int Seasonal,
    int Monthly,
    int Transient,
    int Listed,
    int Maintenance,
    int Vacant
);

// Returned from GetBillingSummaryQuery — invoice KPI aggregates.
public sealed record BillingSummaryDto(
    decimal TotalOutstanding,
    int OverdueCount,
    decimal TotalOverdue,
    decimal CollectedThisMonth,
    int DraftCount,
    int SentCount
);

// Returned from GetMyMarinasQuery — marina summary with the caller's relationship context.
public sealed record MyMarinaDto(
    Guid Id,
    Guid TenantId,
    string Name,
    string? AddressCity,
    string? AddressState,
    string MarinaType,
    bool IsListed,
    bool IsSetupComplete,
    int SetupStep,
    decimal? Latitude,
    decimal? Longitude,
    string? UserRole,           // membership role ("Owner"/"Manager"/"Staff") or billing account role
    string RelationshipKind,    // "Staff" | "BillingAccount"
    string? LogoUrl = null
);
