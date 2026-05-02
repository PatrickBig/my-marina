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

    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
