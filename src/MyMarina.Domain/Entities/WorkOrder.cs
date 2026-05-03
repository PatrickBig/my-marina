using MyMarina.Domain.Enums;

namespace MyMarina.Domain.Entities;

public class WorkOrder
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid MarinaId { get; set; }
    public Guid? MaintenanceRequestId { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public WorkOrderStatus Status { get; set; } = WorkOrderStatus.Open;
    public MaintenancePriority Priority { get; set; } = MaintenancePriority.Medium;
    public DateOnly? ScheduledDate { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    public Marina Marina { get; set; } = null!;
    public MaintenanceRequest? MaintenanceRequest { get; set; }
}
