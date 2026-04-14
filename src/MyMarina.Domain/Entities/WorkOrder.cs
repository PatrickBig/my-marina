using MyMarina.Domain.Common;
using MyMarina.Domain.Enums;

namespace MyMarina.Domain.Entities;

/// <summary>
/// Marina's internal work order. Optionally linked to a customer
/// MaintenanceRequest; can also be created internally with no request.
/// </summary>
public class WorkOrder : TenantEntity
{
    /// <summary>Null when the work order was created internally without a customer request.</summary>
    public Guid? MaintenanceRequestId { get; init; }

    public required string Title { get; set; }
    public required string Description { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public WorkOrderStatus Status { get; set; } = WorkOrderStatus.Open;
    public Priority Priority { get; set; } = Priority.Medium;
    public DateOnly? ScheduledDate { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string? Notes { get; set; }

    public MaintenanceRequest? MaintenanceRequest { get; init; }
}
