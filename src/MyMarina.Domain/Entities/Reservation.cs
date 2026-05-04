using MyMarina.Domain.Enums;
using MyMarina.Domain.ValueObjects;

namespace MyMarina.Domain.Entities;

public class Reservation
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public Guid BoaterUserId { get; set; }
    public Guid VesselId { get; set; }
    public Guid SlipId { get; set; }
    public Guid AvailabilityWindowId { get; set; }

    public DateTimeOffset ArrivesAt { get; set; }
    public DateTimeOffset DepartsAt { get; set; }

    public ReservationStatus Status { get; set; }

    // Price snapshot — computed and frozen at booking time
    public decimal BasePrice { get; set; }
    public decimal Fees { get; set; }
    public decimal Taxes { get; set; }
    public decimal Total { get; set; }

    // Frozen copy of AvailabilityWindow.RevenueSplit at booking time
    public List<RevenueSplitEntry> RevenueSplitSnapshot { get; set; } = [];

    // Frozen cancellation policy (null in MVP — off-platform reconciliation)
    public string? CancellationPolicySnapshot { get; set; }

    // Era 2 — Stripe Connect placeholders (null in MVP)
    public string? PaymentIntentId { get; set; }
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.OffPlatform;
    public decimal PlatformFeeAmount { get; set; }

    public DateTimeOffset RequestedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ConfirmedAt { get; set; }
    public DateTimeOffset? DeclinedAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public Guid? CancelledByUserId { get; set; }

    public string? Notes { get; set; }

    // Navigation
    public AvailabilityWindow AvailabilityWindow { get; set; } = null!;
    public Vessel Vessel { get; set; } = null!;
    public Slip Slip { get; set; } = null!;
}
