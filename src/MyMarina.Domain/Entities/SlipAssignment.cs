using MyMarina.Domain.Enums;

namespace MyMarina.Domain.Entities;

public class SlipAssignment
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public Guid SlipId { get; set; }
    public Guid BillingAccountId { get; set; }
    public Guid VesselId { get; set; }

    public AssignmentType AssignmentType { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public decimal BaseRate { get; set; }

    // Sublet policy flags — negotiated at lease signing
    public bool AllowOwnerSubletWhenAway { get; set; }
    public bool AllowHolderSublet { get; set; }
    public decimal OwnerSubletShareToHolder { get; set; }
    public decimal HolderSubletShareToOwner { get; set; }

    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    // Navigation
    public Slip Slip { get; set; } = null!;
    public BillingAccount BillingAccount { get; set; } = null!;
    public Vessel Vessel { get; set; } = null!;
}
