using MyMarina.Domain.Enums;

namespace MyMarina.Domain.Entities;

public class MaintenanceRequest
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid MarinaId { get; set; }
    public Guid BoaterUserId { get; set; }
    public Guid? BillingAccountId { get; set; }
    public Guid? VesselId { get; set; }
    public Guid? SlipId { get; set; }
    public Guid? ReservationId { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public MaintenanceRequestStatus Status { get; set; } = MaintenanceRequestStatus.Submitted;
    public MaintenancePriority Priority { get; set; } = MaintenancePriority.Medium;
    public DateTimeOffset SubmittedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ResolvedAt { get; set; }

    public Marina Marina { get; set; } = null!;
    public WorkOrder? WorkOrder { get; set; }
}
