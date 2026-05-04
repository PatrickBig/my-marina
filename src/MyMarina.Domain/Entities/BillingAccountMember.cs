using MyMarina.Domain.Enums;

namespace MyMarina.Domain.Entities;

public class BillingAccountMember
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid BillingAccountId { get; set; }
    public Guid UserId { get; set; }
    public BillingAccountRole Role { get; set; }
    public DateTimeOffset InvitedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? AcceptedAt { get; set; }
    public Guid? InvitedByUserId { get; set; }

    // Navigation
    public BillingAccount BillingAccount { get; set; } = null!;
}
