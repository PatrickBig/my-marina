using MyMarina.Domain.Common;
using MyMarina.Domain.Enums;

namespace MyMarina.Domain.Entities;

/// <summary>
/// Links a slip to a customer account and boat for a period of time.
/// </summary>
public class SlipAssignment : TenantEntity
{
    public Guid SlipId { get; init; }
    public Guid CustomerAccountId { get; init; }
    public Guid BoatId { get; init; }
    public AssignmentType AssignmentType { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    /// <summary>Overrides the slip's standard rate when set.</summary>
    public decimal? RateOverride { get; set; }

    public string? Notes { get; set; }

    public Slip Slip { get; init; } = null!;
    public CustomerAccount CustomerAccount { get; init; } = null!;
    public Boat Boat { get; init; } = null!;
}
