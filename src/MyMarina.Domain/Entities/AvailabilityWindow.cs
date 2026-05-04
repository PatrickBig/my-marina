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

    public ListingKind ListingKind { get; set; } = ListingKind.Transient;
    public LeaseTerm? LeaseTerm { get; set; }       // required when ListingKind = Lease

    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }

    public bool InstantBook { get; set; }

    public int? MinNights { get; set; }
    public int? MaxNights { get; set; }

    // Rate: how BasePricePerNight is interpreted depends on RateKind + ListingKind:
    //   Transient + Flat    = $/night
    //   Transient + PerFoot = $/ft/night
    //   Lease     + Flat    = $/period (period defined by LeaseTerm)
    //   Lease     + PerFoot = $/ft/period
    public RateKind RateKind { get; set; } = RateKind.Flat;
    public decimal BasePricePerNight { get; set; }  // holds the rate regardless of kind/period
    public decimal? MinCharge { get; set; }         // floor applied after per-foot calculation
    public decimal? WeeklyDiscount { get; set; }    // fraction 0–1; applied for stays ≥ 7 nights (Transient only)
    public decimal? MonthlyDiscount { get; set; }   // fraction 0–1; applied for stays ≥ 28 nights (Transient only)
    public decimal? CleaningFee { get; set; }

    // Stored as JSON array in the database
    public List<RevenueSplitEntry> RevenueSplit { get; set; } = [];

    public AvailabilityWindowStatus Status { get; set; } = AvailabilityWindowStatus.Open;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    // Navigation
    public Slip Slip { get; set; } = null!;
}
