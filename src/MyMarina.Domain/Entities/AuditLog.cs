namespace MyMarina.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid? TenantId { get; set; }
    public Guid? ActorUserId { get; set; }
    public required string Action { get; set; }
    public string? TargetType { get; set; }
    public string? TargetId { get; set; }
    public string? Details { get; set; }
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
