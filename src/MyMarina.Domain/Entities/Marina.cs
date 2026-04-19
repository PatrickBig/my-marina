using MyMarina.Domain.Common;
using MyMarina.Domain.ValueObjects;

namespace MyMarina.Domain.Entities;

/// <summary>
/// An individual marina facility. Tenant-scoped.
/// </summary>
public class Marina : TenantEntity
{
    public required string Name { get; set; }
    public required Address Address { get; set; }
    public required string PhoneNumber { get; set; }
    public required string Email { get; set; }
    public string? Website { get; set; }
    public string? Description { get; set; }

    /// <summary>IANA timezone ID, e.g. "America/New_York".</summary>
    public required string TimeZoneId { get; set; }

    // Health threshold targets — explicit two-level thresholds per metric
    public decimal? OccupancyWarningThreshold { get; set; }
    public decimal? OccupancyAlertThreshold { get; set; }
    public int? OverdueWarningDays { get; set; }
    public int? OverdueAlertDays { get; set; }

    public ICollection<Dock> Docks { get; init; } = [];
    public ICollection<Slip> Slips { get; init; } = [];
    public ICollection<Announcement> Announcements { get; init; } = [];
}
