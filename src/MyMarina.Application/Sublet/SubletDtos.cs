namespace MyMarina.Application.Sublet;

public sealed record OwnerAbsenceDto(
    Guid Id,
    Guid SlipAssignmentId,
    Guid SlipId,
    string SlipName,
    DateOnly StartsOn,
    DateOnly EndsOn,
    string? Notes,
    DateTimeOffset CreatedAt);

public sealed record MySlipAssignmentDto(
    Guid Id,
    Guid SlipId,
    string SlipName,
    string SlipType,
    Guid MarinaId,
    string MarinaName,
    Guid BillingAccountId,
    Guid VesselId,
    string VesselName,
    string AssignmentType,
    DateOnly StartDate,
    DateOnly? EndDate,
    decimal BaseRate,
    bool AllowHolderSublet,
    bool AllowOwnerSubletWhenAway,
    bool IsActive);
