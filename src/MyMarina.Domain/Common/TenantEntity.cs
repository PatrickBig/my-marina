namespace MyMarina.Domain.Common;

/// <summary>
/// Base class for all tenant-scoped entities. EF Core global query filters
/// use TenantId to enforce row-level isolation between tenants automatically.
/// </summary>
public abstract class TenantEntity
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid TenantId { get; init; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
}
