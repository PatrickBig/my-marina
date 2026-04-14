namespace MyMarina.Domain.Entities;

/// <summary>
/// Append-only record of all mutations. No deletes, no updates.
/// TenantId is null for platform operator actions.
/// </summary>
public class AuditLog
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid? TenantId { get; init; }
    public Guid UserId { get; init; }

    /// <summary>e.g., "slip.assigned", "invoice.created", "payment.recorded"</summary>
    public required string Action { get; init; }

    /// <summary>e.g., "Slip", "Invoice"</summary>
    public required string EntityType { get; init; }

    public Guid EntityId { get; init; }

    /// <summary>Previous state as JSON; null for creates.</summary>
    public string? Before { get; init; }

    /// <summary>New state as JSON; null for deletes.</summary>
    public string? After { get; init; }

    public string? IpAddress { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
