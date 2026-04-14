using MyMarina.Domain.Common;
using MyMarina.Domain.Enums;

namespace MyMarina.Domain.Entities;

/// <summary>
/// An individual boat berth or mooring. DockId is nullable — a null DockId
/// indicates a free-standing mooring or anchorage with no dock parent.
/// MarinaId is always set regardless.
/// </summary>
public class Slip : TenantEntity
{
    public Guid MarinaId { get; init; }

    /// <summary>Null for free-standing moorings and anchorages.</summary>
    public Guid? DockId { get; init; }

    public required string Name { get; set; }
    public SlipType SlipType { get; set; }
    public decimal MaxLength { get; set; }
    public decimal MaxBeam { get; set; }
    public decimal MaxDraft { get; set; }
    public bool HasElectric { get; set; }
    public ElectricService? Electric { get; set; }
    public bool HasWater { get; set; }
    public RateType RateType { get; set; }
    public decimal? DailyRate { get; set; }
    public decimal? MonthlyRate { get; set; }
    public decimal? AnnualRate { get; set; }
    public SlipStatus Status { get; set; } = SlipStatus.Available;

    /// <summary>GPS latitude — useful for mooring/anchorage map views.</summary>
    public decimal? Latitude { get; set; }

    /// <summary>GPS longitude — useful for mooring/anchorage map views.</summary>
    public decimal? Longitude { get; set; }

    public string? Notes { get; set; }

    public Marina Marina { get; init; } = null!;
    public Dock? Dock { get; init; }
    public ICollection<SlipAssignment> Assignments { get; init; } = [];
}
