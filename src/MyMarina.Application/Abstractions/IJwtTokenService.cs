namespace MyMarina.Application.Abstractions;

public interface IJwtTokenService
{
    string GenerateAccessToken(UserTokenInfo user, int? expiryMinutesOverride = null);
}

public sealed record UserTokenInfo(
    Guid UserId,
    string Email,
    bool EmailVerified,
    string FirstName,
    string LastName,
    bool IsPlatformOperator,
    bool IsDemo,
    IReadOnlyList<MembershipClaim> Memberships,
    IReadOnlyList<BillingAccountMemberClaim> BillingAccounts
);
