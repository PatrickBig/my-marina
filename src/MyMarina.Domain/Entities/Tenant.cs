using MyMarina.Domain.Enums;

namespace MyMarina.Domain.Entities;

/// <summary>
/// Top-level corporate/billing entity. A Tenant may own multiple Marinas
/// (e.g., a marina management company). MVP enforces one Marina per Tenant
/// at the business logic layer, not the schema.
/// </summary>
public class Tenant
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public SubscriptionTier SubscriptionTier { get; set; } = SubscriptionTier.Free;
    public bool IsActive { get; set; } = true;
    public bool IsDemo { get; set; } = false;
    public DateTimeOffset? DemoExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    public ICollection<Marina> Marinas { get; init; } = [];
}
