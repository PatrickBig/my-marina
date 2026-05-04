using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Memberships;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Memberships;

public class UpdateMembershipRoleCommandHandler(AppDbContext db)
    : ICommandHandler<UpdateMembershipRoleCommand, MembershipDto>
{
    public async Task<MembershipDto> HandleAsync(UpdateMembershipRoleCommand command, CancellationToken ct = default)
    {
        var membership = await db.Memberships
            .FirstOrDefaultAsync(m => m.Id == command.MembershipId && m.MarinaId == command.MarinaId, ct)
            ?? throw new KeyNotFoundException("Membership not found.");

        membership.Role = command.NewRole;
        await db.SaveChangesAsync(ct);

        return MembershipMappers.ToDto(membership, null, null, null);
    }
}
