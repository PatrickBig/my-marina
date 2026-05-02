using MyMarina.Domain.Enums;

namespace MyMarina.Domain.Entities;

public class BillingAccount
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid MarinaId { get; set; }

    public required string DisplayName { get; set; }
    public required string BillingEmail { get; set; }
    public string? BillingPhone { get; set; }

    // Billing address — flat columns
    public string? BillingAddressStreet { get; set; }
    public string? BillingAddressCity { get; set; }
    public string? BillingAddressState { get; set; }
    public string? BillingAddressZip { get; set; }
    public string? BillingAddressCountry { get; set; }

    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }

    // Marina-private notes; never exposed to the account holder
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    // Navigation
    public Marina Marina { get; set; } = null!;
    public ICollection<BillingAccountMember> Members { get; set; } = [];
    public ICollection<MarinaVesselRecord> VesselRecords { get; set; } = [];
}
