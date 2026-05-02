namespace MyMarina.Application.SlipAssignments;

public sealed record SlipAssignmentDto(
    Guid Id,
    Guid SlipId,
    string SlipName,
    Guid BillingAccountId,
    string BillingAccountDisplayName,
    Guid VesselId,
    string VesselName,
    string AssignmentType,
    DateOnly StartDate,
    DateOnly? EndDate,
    decimal BaseRate,
    bool AllowOwnerSubletWhenAway,
    bool AllowHolderSublet,
    decimal OwnerSubletShareToHolder,
    decimal HolderSubletShareToOwner,
    string? Notes,
    bool IsActive,
    DateTimeOffset CreatedAt
);

public sealed record SlipAvailabilityResult(
    bool IsAvailable,
    IReadOnlyList<SlipAssignmentConflict> Conflicts
);

public sealed record SlipAssignmentConflict(
    Guid AssignmentId,
    string BillingAccountDisplayName,
    DateOnly StartDate,
    DateOnly? EndDate
);
