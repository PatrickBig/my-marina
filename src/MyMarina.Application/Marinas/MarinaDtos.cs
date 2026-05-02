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
    DateTimeOffset CreatedAt
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
    string Status,
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
