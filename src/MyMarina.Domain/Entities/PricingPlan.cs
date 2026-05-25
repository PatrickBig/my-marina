using MyMarina.Domain.Enums;
using MyMarina.Domain.ValueObjects;

namespace MyMarina.Domain.Entities;

public class PricingPlan
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid MarinaId { get; set; }
    public Guid TenantId { get; set; }
    public required string Name { get; set; }
    public bool IsDefault { get; set; }

    // Transient (nightly) rate — null means plan does not support transient bookings.
    public RateKind? TransientRateKind { get; set; }
    public decimal? TransientAmount { get; set; }
    public decimal? TransientMinCharge { get; set; }

    // Lease rate — null means plan does not support lease bookings.
    public RateKind? LeaseRateKind { get; set; }
    public decimal? LeaseAmount { get; set; }
    public decimal? LeaseMinCharge { get; set; }

    // Flat add-on amounts per amenity, stored as JSONB.
    public ICollection<AmenityAddOn> AmenityAddOns { get; set; } = [];

    public Marina Marina { get; set; } = null!;

    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
