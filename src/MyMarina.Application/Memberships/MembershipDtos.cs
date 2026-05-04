namespace MyMarina.Application.Memberships;

public sealed record MembershipDto(
    Guid Id,
    Guid UserId,
    string? UserEmail,
    string? UserFirstName,
    string? UserLastName,
    Guid TenantId,
    Guid? MarinaId,
    string Scope,
    string Role,
    DateTimeOffset InvitedAt,
    DateTimeOffset? AcceptedAt,
    bool IsPending
);
