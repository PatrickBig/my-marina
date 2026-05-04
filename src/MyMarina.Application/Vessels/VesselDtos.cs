namespace MyMarina.Application.Vessels;

public sealed record VesselDto(
    Guid Id,
    string Name,
    string? Make,
    string? Model,
    int? Year,
    decimal Length,
    decimal Beam,
    decimal Draft,
    string BoatType,
    string? HullColor,
    string? RegistrationNumber,
    string? RegistrationState,
    bool IsArchived,
    DateTimeOffset CreatedAt
);

public sealed record PendingVesselClaimDto(
    Guid VesselId,
    string Name,
    string? Make,
    string? Model,
    int? Year,
    decimal Length,
    string BoatType,
    // Marina that added this vessel
    Guid MarinaId,
    string MarinaName
);
