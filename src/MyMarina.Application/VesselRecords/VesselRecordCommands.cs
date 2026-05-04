using MyMarina.Domain.Enums;

namespace MyMarina.Application.VesselRecords;

/// <summary>
/// Creates a MarinaVesselRecord linking an existing Vessel to a Marina.
/// If VesselId is null, creates a ghost Vessel using the provided fields
/// and sends a claim invitation email to ClaimEmail.
/// </summary>
public sealed record CreateVesselRecordCommand(
    Guid MarinaId,
    Guid RequestingUserId,
    Guid? BillingAccountId,
    // Existing vessel
    Guid? VesselId,
    // Ghost vessel fields (used when VesselId is null)
    string? ClaimEmail,
    string? VesselName,
    string? VesselMake,
    string? VesselModel,
    int? VesselYear,
    decimal? VesselLength,
    decimal? VesselBeam,
    decimal? VesselDraft,
    BoatType? VesselBoatType,
    string? VesselHullColor,
    string? VesselRegistrationNumber,
    string? VesselRegistrationState,
    // Insurance
    string? InsuranceProvider,
    string? InsurancePolicyNumber,
    DateOnly? InsuranceExpiresOn,
    string? Notes
);

public sealed record UpdateVesselRecordCommand(
    Guid Id,
    Guid MarinaId,
    Guid RequestingUserId,
    Guid? BillingAccountId,
    string? InsuranceProvider,
    string? InsurancePolicyNumber,
    DateOnly? InsuranceExpiresOn,
    bool? MarkInsuranceVerified,
    string? Notes
);

public sealed record GetVesselRecordsQuery(Guid MarinaId, Guid RequestingUserId, Guid? BillingAccountId = null);

public sealed record GetVesselRecordQuery(Guid Id, Guid MarinaId, Guid RequestingUserId);
