using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Memberships;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Memberships;

public class AcceptMembershipCommandHandler(AppDbContext db)
    : ICommandHandler<AcceptMembershipCommand>
{
    public async Task HandleAsync(AcceptMembershipCommand command, CancellationToken ct = default)
    {
        var membership = await db.Memberships
            .FirstOrDefaultAsync(m => m.Id == command.MembershipId, ct)
            ?? throw new KeyNotFoundException("Membership invitation not found.");

        // Ghost invitations (UserId == Guid.Empty) need to be claimed at this point
        if (membership.UserId == Guid.Empty)
            membership.UserId = command.UserId;
        else if (membership.UserId != command.UserId)
            throw new UnauthorizedAccessException("This invitation was not sent to you.");

        if (membership.AcceptedAt.HasValue)
            return; // Already accepted — idempotent

        membership.AcceptedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }
}
