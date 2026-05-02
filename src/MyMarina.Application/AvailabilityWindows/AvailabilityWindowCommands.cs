using MyMarina.Domain.Enums;

namespace MyMarina.Application.AvailabilityWindows;

public sealed record CreateAvailabilityWindowCommand(
    Guid MarinaId,
    Guid RequestingUserId,
    Guid SlipId,
    ListedByKind ListedByKind,
    Guid? ListedByMarinaId,
    Guid? ListedByBillingAccountId,
    Guid? RelatedAssignmentId,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    bool InstantBook,
    int? MinNights,
    int? MaxNights,
    decimal BasePricePerNight,
    decimal? WeeklyDiscount,
    decimal? MonthlyDiscount,
    decimal? CleaningFee
);

public sealed record UpdateAvailabilityWindowCommand(
    Guid Id,
    Guid MarinaId,
    Guid RequestingUserId,
    DateTimeOffset? StartsAt,
    DateTimeOffset? EndsAt,
    bool? InstantBook,
    int? MinNights,
    int? MaxNights,
    decimal? BasePricePerNight,
    decimal? WeeklyDiscount,
    decimal? MonthlyDiscount,
    decimal? CleaningFee
);

public sealed record SetAvailabilityWindowStatusCommand(
    Guid Id,
    Guid MarinaId,
    Guid RequestingUserId,
    AvailabilityWindowStatus Status
);

public sealed record GetAvailabilityWindowsQuery(
    Guid MarinaId,
    Guid RequestingUserId,
    Guid? SlipId = null,
    string? Status = null
);

public sealed record GetAvailabilityWindowQuery(Guid Id, Guid MarinaId, Guid RequestingUserId);
