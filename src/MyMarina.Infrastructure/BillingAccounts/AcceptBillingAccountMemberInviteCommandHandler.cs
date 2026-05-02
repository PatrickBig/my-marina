using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.BillingAccounts;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.BillingAccounts;

public class AcceptBillingAccountMemberInviteCommandHandler(AppDbContext db)
    : ICommandHandler<AcceptBillingAccountMemberInviteCommand, BillingAccountMemberDto>
{
    public async Task<BillingAccountMemberDto> HandleAsync(AcceptBillingAccountMemberInviteCommand command, CancellationToken ct = default)
    {
        var member = await db.BillingAccountMembers
            .FirstOrDefaultAsync(m => m.Id == command.BillingAccountMemberId, ct)
            ?? throw new KeyNotFoundException($"BillingAccountMember {command.BillingAccountMemberId} not found.");

        if (member.AcceptedAt.HasValue)
            throw new InvalidOperationException("Invitation has already been accepted.");

        member.UserId     = command.UserId;
        member.AcceptedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);

        return new BillingAccountMemberDto(
            Id:               member.Id,
            BillingAccountId: member.BillingAccountId,
            UserId:           member.UserId,
            Role:             member.Role.ToString(),
            InvitedAt:        member.InvitedAt,
            AcceptedAt:       member.AcceptedAt
        );
    }
}
