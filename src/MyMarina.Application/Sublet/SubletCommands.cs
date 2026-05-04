namespace MyMarina.Application.Sublet;

// ─── Boater: "I'm away" ───────────────────────────────────────────────────────

public sealed record CreateOwnerAbsenceCommand(
    Guid SlipAssignmentId,
    Guid RequestingUserId,
    IReadOnlyList<Guid> RequestingUserBillingAccountIds,
    DateOnly StartsOn,
    DateOnly EndsOn,
    string? Notes);

public sealed record DeleteOwnerAbsenceCommand(
    Guid Id,
    Guid SlipAssignmentId,
    Guid RequestingUserId,
    IReadOnlyList<Guid> RequestingUserBillingAccountIds);

public sealed record GetOwnerAbsencesQuery(
    Guid SlipAssignmentId,
    Guid RequestingUserId,
    IReadOnlyList<Guid> RequestingUserBillingAccountIds);

// ─── Marina: see all absences at their marina ─────────────────────────────────

public sealed record GetMarinaAbsencesQuery(
    Guid MarinaId,
    Guid RequestingUserId,
    Guid? SlipId = null);

// ─── Boater: see their own slip assignments ───────────────────────────────────

public sealed record GetMySlipAssignmentsQuery(
    Guid RequestingUserId,
    IReadOnlyList<Guid> BillingAccountIds);

// ─── Holder: create their own sublet listing ─────────────────────────────────

public sealed record CreateHolderSubletWindowCommand(
    Guid SlipAssignmentId,
    Guid RequestingUserId,
    IReadOnlyList<Guid> RequestingUserBillingAccountIds,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    bool InstantBook,
    int? MinNights,
    int? MaxNights,
    decimal BasePricePerNight,
    decimal? WeeklyDiscount,
    decimal? MonthlyDiscount,
    decimal? CleaningFee);
