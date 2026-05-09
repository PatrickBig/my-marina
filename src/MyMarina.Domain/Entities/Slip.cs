using MyMarina.Domain.Enums;

namespace MyMarina.Domain.Entities;

public class Slip
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid MarinaId { get; set; }
    public Guid? HostMarinaId { get; set; }
    public HostMarinaPolicy HostMarinaPolicy { get; set; } = HostMarinaPolicy.None;
    public Guid? DockId { get; set; }
    public required string Name { get; set; }
    public SlipType SlipType { get; set; } = SlipType.Floating;
    public decimal MaxLength { get; set; }
    public decimal MaxBeam { get; set; }
    public decimal MaxDraft { get; set; }
    public bool HasElectric { get; set; }
    public ElectricAmperage? Electric { get; set; }
    public bool HasWater { get; set; }
    public bool HasPumpOut { get; set; }
    public bool IsCovered { get; set; }
    public bool IsIndoor { get; set; }
    public List<string> Amenities { get; set; } = [];
    public SlipStatus Status { get; set; } = SlipStatus.Active;

    // Location — required for marketplace, optional during initial setup
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    // Address override for private docks (null = inherit from marina)
    public string? AddressStreet { get; set; }
    public string? AddressCity { get; set; }
    public string? AddressState { get; set; }
    public string? AddressZip { get; set; }
    public string? AddressCountry { get; set; }

    // Transient default rate — when set, slip is implicitly available for transient booking
    // any time it has no active SlipAssignment or confirmed Reservation.
    public RateKind? DefaultTransientRateKind { get; set; }   // null = not listed for transient
    public decimal? DefaultTransientBaseRate { get; set; }
    public decimal? DefaultTransientMinCharge { get; set; }

    // Lease default rate — when set, slip appears in lease searches.
    public RateKind? DefaultLeaseRateKind { get; set; }       // null = not listed for lease
    public decimal? DefaultLeaseBaseRate { get; set; }
    public LeaseTerm? DefaultLeaseTerm { get; set; }

    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
