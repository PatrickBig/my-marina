using MyMarina.Domain.Enums;

namespace MyMarina.Application.Vessels;

public sealed record CreateVesselCommand(
    Guid OwnerId,
    string Name,
    string? Make,
    string? Model,
    int? Year,
    decimal Length,
    decimal Beam,
    decimal Draft,
    BoatType BoatType,
    string? HullColor,
    string? RegistrationNumber,
    string? RegistrationState
);

public sealed record UpdateVesselCommand(
    Guid Id,
    Guid OwnerId,
    string? Name,
    string? Make,
    string? Model,
    int? Year,
    decimal? Length,
    decimal? Beam,
    decimal? Draft,
    BoatType? BoatType,
    string? HullColor,
    string? RegistrationNumber,
    string? RegistrationState
);

public sealed record ArchiveVesselCommand(Guid Id, Guid OwnerId);

public sealed record GetMyVesselsQuery(Guid OwnerId);

public sealed record GetVesselQuery(Guid Id, Guid OwnerId);

// Ghost vessel claim flow
public sealed record GetPendingVesselClaimsQuery(Guid UserId, string UserEmail);

public sealed record ClaimVesselCommand(Guid VesselId, Guid UserId, string UserEmail);

public sealed record RejectVesselClaimCommand(Guid VesselId, Guid UserId, string UserEmail);
