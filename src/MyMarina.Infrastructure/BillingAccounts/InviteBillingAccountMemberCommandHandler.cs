using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.BillingAccounts;
using MyMarina.Domain.Entities;
using MyMarina.Infrastructure.Identity;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.BillingAccounts;

public class InviteBillingAccountMemberCommandHandler(
    AppDbContext db,
    UserManager<ApplicationUser> userManager,
    IEmailService emailService)
    : ICommandHandler<InviteBillingAccountMemberCommand, BillingAccountMemberDto>
{
    public async Task<BillingAccountMemberDto> HandleAsync(InviteBillingAccountMemberCommand command, CancellationToken ct = default)
    {
        var account = await db.BillingAccounts
            .FirstOrDefaultAsync(a => a.Id == command.BillingAccountId && a.MarinaId == command.MarinaId, ct)
            ?? throw new KeyNotFoundException($"BillingAccount {command.BillingAccountId} not found.");

        var invitedUser = await userManager.FindByEmailAsync(command.Email);

        if (invitedUser is not null)
        {
            var existing = await db.BillingAccountMembers
                .FirstOrDefaultAsync(m => m.BillingAccountId == command.BillingAccountId && m.UserId == invitedUser.Id, ct);
            if (existing is not null)
                throw new InvalidOperationException($"User {command.Email} is already a member of this billing account.");
        }

        var invitedBy = await userManager.FindByIdAsync(command.RequestingUserId.ToString())
            ?? throw new KeyNotFoundException("Requesting user not found.");

        var member = new BillingAccountMember
        {
            BillingAccountId  = command.BillingAccountId,
            // If user is already on platform, link immediately; otherwise use a placeholder
            UserId            = invitedUser?.Id ?? Guid.Empty,
            Role              = command.Role,
            InvitedByUserId   = command.RequestingUserId,
            // If user is already on platform, auto-accept
            AcceptedAt        = invitedUser is not null ? DateTimeOffset.UtcNow : null,
        };

        db.BillingAccountMembers.Add(member);
        await db.SaveChangesAsync(ct);

        var marinaName = await db.Marinas
            .Where(m => m.Id == command.MarinaId)
            .Select(m => m.Name)
            .FirstOrDefaultAsync(ct) ?? "the marina";

        await emailService.SendBillingAccountInviteAsync(
            toEmail:       command.Email,
            marinaName:    marinaName,
            invitedByName: $"{invitedBy.FirstName} {invitedBy.LastName}",
            memberId:      member.Id,
            ct:            ct);

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
