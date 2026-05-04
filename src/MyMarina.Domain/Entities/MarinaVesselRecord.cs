namespace MyMarina.Domain.Entities;

public class MarinaVesselRecord
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid MarinaId { get; set; }
    public Guid VesselId { get; set; }
    public Guid? BillingAccountId { get; set; }

    public string? InsuranceProvider { get; set; }
    public string? InsurancePolicyNumber { get; set; }
    public DateOnly? InsuranceExpiresOn { get; set; }
    public DateTimeOffset? InsuranceVerifiedAt { get; set; }
    public Guid? InsuranceVerifiedByUserId { get; set; }

    // Marina-private notes; never visible to vessel owner
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    // Navigation
    public Marina Marina { get; set; } = null!;
    public Vessel Vessel { get; set; } = null!;
    public BillingAccount? BillingAccount { get; set; }
}
