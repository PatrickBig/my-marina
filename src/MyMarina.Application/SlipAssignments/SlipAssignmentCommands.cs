using MyMarina.Domain.Enums;

namespace MyMarina.Application.SlipAssignments;

public sealed record CreateSlipAssignmentCommand(
    Guid MarinaId,
    Guid RequestingUserId,
    Guid SlipId,
    Guid BillingAccountId,
    Guid VesselId,
    AssignmentType AssignmentType,
    DateOnly StartDate,
    DateOnly? EndDate,
    decimal BaseRate,
    bool AllowOwnerSubletWhenAway,
    bool AllowHolderSublet,
    decimal OwnerSubletShareToHolder,
    decimal HolderSubletShareToOwner,
    string? Notes
);

public sealed record UpdateSlipAssignmentCommand(
    Guid Id,
    Guid MarinaId,
    Guid RequestingUserId,
    AssignmentType? AssignmentType,
    DateOnly? StartDate,
    DateOnly? EndDate,
    decimal? BaseRate,
    bool? AllowOwnerSubletWhenAway,
    bool? AllowHolderSublet,
    decimal? OwnerSubletShareToHolder,
    decimal? HolderSubletShareToOwner,
    string? Notes
);

public sealed record EndSlipAssignmentCommand(
    Guid Id,
    Guid MarinaId,
    Guid RequestingUserId,
    DateOnly EndDate
);

public sealed record GetSlipAssignmentsQuery(
    Guid MarinaId,
    Guid RequestingUserId,
    Guid? SlipId = null,
    Guid? BillingAccountId = null,
    bool ActiveOnly = false
);

public sealed record GetSlipAssignmentQuery(Guid Id, Guid MarinaId, Guid RequestingUserId);

// Renews an existing assignment into a new period, resolving price via IPriceResolver.
// Returns null BaseRate resolving to 409 is handled by the handler.
public sealed record RenewSlipAssignmentCommand(
    Guid ExistingAssignmentId,
    Guid MarinaId,
    Guid RequestingUserId,
    DateOnly NewStartDate,
    DateOnly? NewEndDate
);

public sealed record CheckSlipAvailabilityQuery(
    Guid SlipId,
    Guid MarinaId,
    DateOnly StartDate,
    DateOnly? EndDate,
    decimal? VesselLength = null,
    decimal? VesselBeam = null,
    decimal? VesselDraft = null,
    Guid? ExcludeAssignmentId = null
);
