using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Memberships;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Memberships;

public class RevokeMembershipCommandHandler(AppDbContext db)
    : ICommandHandler<RevokeMembershipCommand>
{
    public async Task HandleAsync(RevokeMembershipCommand command, CancellationToken ct = default)
    {
        var membership = await db.Memberships
            .FirstOrDefaultAsync(m => m.Id == command.MembershipId && m.MarinaId == command.MarinaId, ct)
            ?? throw new KeyNotFoundException("Membership not found.");

        db.Memberships.Remove(membership);
        await db.SaveChangesAsync(ct);
    }
}
