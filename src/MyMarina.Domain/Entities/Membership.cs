using MyMarina.Domain.Enums;

namespace MyMarina.Domain.Entities;

public class Membership
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid UserId { get; set; }
    public MembershipScope Scope { get; set; }
    public Guid TenantId { get; set; }
    public Guid? MarinaId { get; set; }
    public MembershipRole Role { get; set; }
    public DateTimeOffset InvitedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? AcceptedAt { get; set; }
    public Guid? InvitedByUserId { get; set; }

    // Navigation
    public Tenant Tenant { get; set; } = null!;
}
