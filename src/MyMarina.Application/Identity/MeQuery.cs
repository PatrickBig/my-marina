using MyMarina.Application.Abstractions;

namespace MyMarina.Application.Identity;

public sealed record MeQuery;

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
    IReadOnlyList<MembershipClaim> Memberships,
    IReadOnlyList<BillingAccountMemberClaim> BillingAccounts
);
