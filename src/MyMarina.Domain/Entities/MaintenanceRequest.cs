using MyMarina.Domain.Common;
using MyMarina.Domain.Enums;

namespace MyMarina.Domain.Entities;

/// <summary>
/// A service request submitted by a customer account member.
/// </summary>
public class MaintenanceRequest : TenantEntity
{
    public Guid CustomerAccountId { get; init; }
    public Guid? SlipId { get; set; }
    public Guid? BoatId { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public MaintenanceStatus Status { get; set; } = MaintenanceStatus.Submitted;
    public Priority Priority { get; set; } = Priority.Medium;
    public DateTimeOffset SubmittedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ResolvedAt { get; set; }

    public CustomerAccount CustomerAccount { get; init; } = null!;
    public WorkOrder? WorkOrder { get; init; }
}
