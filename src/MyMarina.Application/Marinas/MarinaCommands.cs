using MyMarina.Domain.Enums;

namespace MyMarina.Application.Marinas;

// Signup — creates Tenant + Marina + Owner Membership, returns a fresh JWT
public sealed record CreateMarinaAccountCommand(
    Guid UserId,
    string TenantName,
    string MarinaName,
    MarinaType MarinaType,
    string? IpAddress
);

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
    string? TimeZoneId
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
    SlipStatus? Status,
    // Transient default rate (send null to clear)
    string? DefaultTransientRateKind,
    decimal? DefaultTransientBaseRate,
    decimal? DefaultTransientMinCharge,
    bool ClearTransientRate,
    // Lease default rate (send null to clear)
    string? DefaultLeaseRateKind,
    decimal? DefaultLeaseBaseRate,
    string? DefaultLeaseTerm,
    bool ClearLeaseRate,
    string? Notes
);

public sealed record DeleteSlipCommand(Guid SlipId, Guid MarinaId, Guid RequestingUserId);

// Queries
public sealed record GetMarinaQuery(Guid MarinaId, Guid RequestingUserId);
public sealed record GetMyMarinasQuery;
public sealed record GetDocksQuery(Guid MarinaId, Guid RequestingUserId);
public sealed record GetSlipsQuery(Guid MarinaId, Guid RequestingUserId, Guid? DockId = null);
public sealed record GetSlipQuery(Guid SlipId, Guid MarinaId, Guid RequestingUserId);
