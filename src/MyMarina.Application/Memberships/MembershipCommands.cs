using MyMarina.Domain.Enums;

namespace MyMarina.Application.Memberships;

public sealed record InviteStaffCommand(
    Guid MarinaId,
    Guid TenantId,
    Guid InvitedByUserId,
    string Email,
    MembershipRole Role
);

public sealed record AcceptMembershipCommand(Guid MembershipId, Guid UserId);

public sealed record UpdateMembershipRoleCommand(Guid MembershipId, Guid MarinaId, Guid RequestingUserId, MembershipRole NewRole);

public sealed record RevokeMembershipCommand(Guid MembershipId, Guid MarinaId, Guid RequestingUserId);

public sealed record GetMarinaStaffQuery(Guid MarinaId, Guid RequestingUserId);

public sealed record GetMyMembershipsQuery(Guid UserId);
