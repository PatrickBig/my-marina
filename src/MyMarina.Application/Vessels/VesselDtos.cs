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
