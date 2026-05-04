namespace MyMarina.Application.Reservations;

public sealed record CreateReservationCommand(
    Guid RequestingUserId,
    Guid VesselId,
    Guid? AvailabilityWindowId,   // null = book via slip's DefaultNightlyRate
    Guid? SlipId,                  // required when AvailabilityWindowId is null
    DateTimeOffset ArrivesAt,
    DateTimeOffset DepartsAt,
    string? Notes
);

public sealed record ApproveReservationCommand(
    Guid Id,
    Guid MarinaId,        // the marina the approver belongs to (used to determine which approval gate)
    Guid RequestingUserId
);

public sealed record DeclineReservationCommand(
    Guid Id,
    Guid MarinaId,
    Guid RequestingUserId
);

public sealed record CancelReservationCommand(
    Guid Id,
    Guid RequestingUserId
);

public sealed record MarkNoShowCommand(
    Guid Id,
    Guid MarinaId,
    Guid RequestingUserId
);

public sealed record GetMyTripsQuery(
    Guid BoaterUserId,
    string? StatusFilter = null
);

public sealed record GetMarinaReservationsQuery(
    Guid MarinaId,
    Guid RequestingUserId,
    string? StatusFilter = null
);

public sealed record GetReservationQuery(Guid Id, Guid RequestingUserId);
