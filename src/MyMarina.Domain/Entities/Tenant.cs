using MyMarina.Domain.Enums;

namespace MyMarina.Domain.Entities;

public class Tenant
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public SubscriptionTier SubscriptionTier { get; set; } = SubscriptionTier.Free;
    public string? BillingEmail { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDemo { get; set; }
    public DateTimeOffset? SuspendedAt { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
}
