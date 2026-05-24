using MyMarina.Domain.Enums;

namespace MyMarina.Application.Marinas;

// Signup — creates Tenant + Marina + Owner Membership, returns a fresh JWT.
// For PrivateDock/Dockominium types, also auto-creates the first Slip.
public sealed record CreateMarinaAccountCommand(
    Guid UserId,
    string MarinaName,
    MarinaType MarinaType,
    string? IpAddress,
    string? TenantName      = null,
    // Slip fields — required when MarinaType is PrivateDock or Dockominium
    string? SlipName        = null,
    decimal? MaxLength      = null,
    decimal? MaxBeam        = null,
    decimal? MaxDraft       = null,
    SlipType SlipType       = SlipType.Floating,
    string? SlipNotes       = null,
    // Dockominium-only
    Guid? HostMarinaId           = null,
    HostMarinaPolicy HostMarinaPolicy = HostMarinaPolicy.NotifyOnly
);

// Lightweight marina search for the dockominium onboarding wizard (host marina lookup)
public sealed record SearchMarinasQuery(string? Q, int Limit = 10);

public sealed record UpdateMarinaCommand(
    Guid MarinaId,
    Guid RequestingUserId,
    string? Name,
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
    string? TimeZoneId,
    int? SetupStep = null,
    bool? IsSetupComplete = null,
    bool? IsListed = null
);

// Dock management
public sealed record CreateDockCommand(Guid MarinaId, Guid RequestingUserId, string Name, string? Description, int SortOrder);
public sealed record UpdateDockCommand(Guid DockId, Guid MarinaId, Guid RequestingUserId, string? Name, string? Description, int? SortOrder);
public sealed record DeleteDockCommand(Guid DockId, Guid MarinaId, Guid RequestingUserId);

// Slip management
public sealed record CreateSlipCommand(
    Guid MarinaId,
    Guid RequestingUserId,
    Guid? DockId,
    string Name,
    SlipType SlipType,
    decimal MaxLength,
    decimal MaxBeam,
    decimal MaxDraft,
    bool HasElectric,
    ElectricAmperage? Electric,
    bool HasWater,
    bool HasPumpOut,
    bool IsCovered,
    bool IsIndoor,
    IReadOnlyList<string>? Amenities,
    string? Notes
);

public sealed record UpdateSlipCommand(
    Guid SlipId,
    Guid MarinaId,
    Guid RequestingUserId,
    Guid? DockId,
    string? Name,
    SlipType? SlipType,
    decimal? MaxLength,
    decimal? MaxBeam,
    decimal? MaxDraft,
    bool? HasElectric,
    ElectricAmperage? Electric,
    bool? HasWater,
    bool? HasPumpOut,
    bool? IsCovered,
    bool? IsIndoor,
    IReadOnlyList<string>? Amenities,
    SlipStatus? Status,
    string? Notes,
    Guid? PricingPlanId = null,
    bool ClearPricingPlan = false
);

public sealed record DeleteSlipCommand(Guid SlipId, Guid MarinaId, Guid RequestingUserId);

// Setup / lifecycle
public sealed record SetupDocksCommand(
    Guid MarinaId,
    Guid RequestingUserId,
    IReadOnlyList<SetupDockItem> Docks
);

public sealed record SetupDockItem(
    string Name,
    IReadOnlyList<SetupSlipItem> Slips
);

public sealed record SetupSlipItem(
    string Name,
    decimal MaxLength,
    decimal MaxBeam,
    decimal MaxDraft,
    string SlipType,
    bool HasElectric,
    int? Electric,
    bool HasWater,
    bool HasPumpOut,
    bool IsCovered,
    bool IsIndoor,
    IReadOnlyList<string> Amenities
);

public sealed record DeleteDraftMarinaCommand(Guid MarinaId, Guid RequestingUserId);

// Queries
public sealed record GetMarinaQuery(Guid MarinaId, Guid RequestingUserId);
public sealed record GetMyMarinasQuery;
public sealed record GetDocksQuery(Guid MarinaId, Guid RequestingUserId);
public sealed record GetSlipsQuery(Guid MarinaId, Guid RequestingUserId, Guid? DockId = null);
public sealed record GetSlipQuery(Guid SlipId, Guid MarinaId, Guid RequestingUserId);
