using MyMarina.Application.Abstractions;
using MyMarina.Domain.Enums;

namespace MyMarina.Application.Identity;

public sealed record MeQuery;

public sealed record MeMembershipDto(
    MembershipScope Scope,
    Guid TenantId,
    Guid? MarinaId,
    string? MarinaName,
    MembershipRole Role,
    string? Tier
);

public sealed record MeResponse(
    Guid Id,
    string Email,
    bool EmailConfirmed,
    string FirstName,
    string LastName,
    string? PhoneNumber,
    string? ProfilePhotoUrl,
    bool MarketingOptIn,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastLoginAt,
    IReadOnlyList<MeMembershipDto> Memberships,
    IReadOnlyList<BillingAccountMemberClaim> BillingAccounts,
    bool IsPlatformOperator
);
