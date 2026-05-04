namespace MyMarina.Application.Reservations;

public sealed record ReservationDto(
    Guid Id,
    Guid BoaterUserId,
    Guid VesselId,
    string VesselName,
    Guid SlipId,
    string SlipName,
    Guid MarinaId,
    string MarinaName,
    Guid AvailabilityWindowId,
    DateTimeOffset ArrivesAt,
    DateTimeOffset DepartsAt,
    int Nights,
    string Status,
    decimal BasePrice,
    decimal Fees,
    decimal Taxes,
    decimal Total,
    string PaymentStatus,
    bool InstantBook,
    DateTimeOffset RequestedAt,
    DateTimeOffset? ConfirmedAt,
    DateTimeOffset? DeclinedAt,
    DateTimeOffset? CancelledAt,
    Guid? CancelledByUserId,
    string? Notes
);
