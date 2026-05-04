using MyMarina.Application.Memberships;
using MyMarina.Domain.Entities;

namespace MyMarina.Infrastructure.Memberships;

internal static class MembershipMappers
{
    internal static MembershipDto ToDto(Membership m, string? email, string? firstName, string? lastName) => new(
        Id: m.Id,
        UserId: m.UserId,
        UserEmail: email,
        UserFirstName: firstName,
        UserLastName: lastName,
        TenantId: m.TenantId,
        MarinaId: m.MarinaId,
        Scope: m.Scope.ToString(),
        Role: m.Role.ToString(),
        InvitedAt: m.InvitedAt,
        AcceptedAt: m.AcceptedAt,
        IsPending: m.AcceptedAt is null
    );
}
