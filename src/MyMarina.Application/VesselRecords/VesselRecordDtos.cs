namespace MyMarina.Application.VesselRecords;

public sealed record VesselRecordDto(
    Guid Id,
    Guid MarinaId,
    Guid VesselId,
    Guid? BillingAccountId,
    string VesselName,
    string? VesselMake,
    string? VesselModel,
    int? VesselYear,
    decimal VesselLength,
    string VesselBoatType,
    bool VesselIsGhost,
    string? InsuranceProvider,
    string? InsurancePolicyNumber,
    DateOnly? InsuranceExpiresOn,
    DateTimeOffset? InsuranceVerifiedAt,
    string? Notes,
    DateTimeOffset CreatedAt
);
