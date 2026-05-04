using MyMarina.Domain.Enums;

namespace MyMarina.Domain.Entities;

public class SlipLeaseInquiry
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public Guid SlipId { get; set; }
    public Guid MarinaId { get; set; }          // denormalized for fast marina-scoped queries
    public Guid RequestingUserId { get; set; }
    public Guid? VesselId { get; set; }

    // What the boater is asking for
    public LeaseTerm DesiredTerm { get; set; }
    public DateOnly? DesiredStartDate { get; set; }
    public string? Message { get; set; }

    // Marina-editable fields — marina may adjust before/after approval
    public RateKind? AgreedRateKind { get; set; }
    public decimal? AgreedBaseRate { get; set; }
    public DateOnly? AssignmentStartDate { get; set; }
    public DateOnly? AssignmentEndDate { get; set; }    // null = open-ended
    public string? MarinaNote { get; set; }

    public LeaseInquiryStatus Status { get; set; } = LeaseInquiryStatus.Pending;

    // Audit trail — who touched this and when
    public Guid? ReviewedByUserId { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public Guid? DeclinedByUserId { get; set; }
    public DateTimeOffset? DeclinedAt { get; set; }

    // Populated on approval
    public Guid? SlipAssignmentId { get; set; }
    public Guid? BillingAccountId { get; set; }

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    // Navigation
    public Slip Slip { get; set; } = null!;
}
