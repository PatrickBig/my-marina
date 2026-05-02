using MyMarina.Domain.Enums;
using MyMarina.Domain.ValueObjects;

namespace MyMarina.Domain.Entities;

public class AvailabilityWindow
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public Guid SlipId { get; set; }

    public ListedByKind ListedByKind { get; set; }

    // Set when ListedByKind = Owner or OwnerForHolder (always = Slip.MarinaId)
    public Guid? ListedByMarinaId { get; set; }

    // Set when ListedByKind = Holder (= the holder's SlipAssignment.BillingAccountId)
    public Guid? ListedByBillingAccountId { get; set; }

    // Non-null for Holder and OwnerForHolder kinds
    public Guid? RelatedAssignmentId { get; set; }

    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }

    public bool InstantBook { get; set; }

    public int? MinNights { get; set; }
    public int? MaxNights { get; set; }

    public decimal BasePricePerNight { get; set; }
    public decimal? WeeklyDiscount { get; set; }    // fraction 0–1; applied for stays ≥ 7 nights
    public decimal? MonthlyDiscount { get; set; }   // fraction 0–1; applied for stays ≥ 28 nights
    public decimal? CleaningFee { get; set; }

    // Stored as JSON array in the database
    public List<RevenueSplitEntry> RevenueSplit { get; set; } = [];

    public AvailabilityWindowStatus Status { get; set; } = AvailabilityWindowStatus.Open;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    // Navigation
    public Slip Slip { get; set; } = null!;
}
