using MyMarina.Domain.Enums;

namespace MyMarina.Application.Abstractions;

public interface IUserContext
{
    Guid? UserId { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
    bool IsPlatformOperator { get; }
    bool IsDemo { get; }

    IReadOnlyList<MembershipClaim> Memberships { get; }
    IReadOnlyList<BillingAccountMemberClaim> BillingAccounts { get; }

    bool HasMarinaAccess(Guid marinaId, MembershipRole minimumRole = MembershipRole.Staff);
    bool HasTenantAccess(Guid tenantId, MembershipRole minimumRole = MembershipRole.Staff);
    bool HasBillingAccountAccess(Guid billingAccountId);
}

public record MembershipClaim(
    MembershipScope Scope,
    Guid TenantId,
    Guid? MarinaId,
    MembershipRole Role,
    string? Tier
);

public record BillingAccountMemberClaim(
    Guid BillingAccountId,
    Guid MarinaId,
    BillingAccountRole Role
);
