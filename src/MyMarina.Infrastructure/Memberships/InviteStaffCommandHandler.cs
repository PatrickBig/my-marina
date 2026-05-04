using Microsoft.AspNetCore.Identity;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Memberships;
using MyMarina.Domain.Entities;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Identity;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Memberships;

public class InviteStaffCommandHandler(
    UserManager<ApplicationUser> userManager,
    AppDbContext db,
    IEmailService emailService)
    : ICommandHandler<InviteStaffCommand, MembershipDto>
{
    public async Task<MembershipDto> HandleAsync(InviteStaffCommand command, CancellationToken ct = default)
    {
        var invitedBy = await userManager.FindByIdAsync(command.InvitedByUserId.ToString())
            ?? throw new InvalidOperationException("Inviting user not found.");

        // If the invitee already has an account, find their UserId; otherwise we store a ghost membership
        var targetUser = await userManager.FindByEmailAsync(command.Email);

        var membership = new Membership
        {
            UserId = targetUser?.Id ?? Guid.Empty,
            Scope = MembershipScope.Marina,
            TenantId = command.TenantId,
            MarinaId = command.MarinaId,
            Role = command.Role,
            InvitedByUserId = command.InvitedByUserId,
        };
        db.Memberships.Add(membership);
        await db.SaveChangesAsync(ct);

        var inviterName = $"{invitedBy.FirstName} {invitedBy.LastName}".Trim();
        var marina = await db.Marinas.FindAsync([command.MarinaId], ct);
        var marinaName = marina?.Name ?? "your marina";

        await emailService.SendMembershipInviteAsync(command.Email, marinaName, inviterName, membership.Id, ct);

        return MembershipMappers.ToDto(membership, command.Email, targetUser?.FirstName, targetUser?.LastName);
    }
}
