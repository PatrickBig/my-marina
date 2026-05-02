using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.BillingAccounts;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.BillingAccounts;

public class RemoveBillingAccountMemberCommandHandler(AppDbContext db)
    : ICommandHandler<RemoveBillingAccountMemberCommand>
{
    public async Task HandleAsync(RemoveBillingAccountMemberCommand command, CancellationToken ct = default)
    {
        var member = await db.BillingAccountMembers
            .Include(m => m.BillingAccount)
            .FirstOrDefaultAsync(m => m.Id == command.BillingAccountMemberId
                && m.BillingAccountId == command.BillingAccountId
                && m.BillingAccount.MarinaId == command.MarinaId, ct)
            ?? throw new KeyNotFoundException($"BillingAccountMember {command.BillingAccountMemberId} not found.");

        db.BillingAccountMembers.Remove(member);
        await db.SaveChangesAsync(ct);
    }
}
