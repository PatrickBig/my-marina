using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Staff;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Email;
using MyMarina.Infrastructure.Identity;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Staff;

public class InviteStaffCommandHandler(
    UserManager<ApplicationUser> userManager,
    ITenantContext tenantContext,
    AppDbContext db,
    IEmailService emailService,
    IOptions<EmailOptions> emailOptions,
    ILogger<InviteStaffCommandHandler> logger) : ICommandHandler<InviteStaffCommand, InviteStaffResult>
{
    public async Task<InviteStaffResult> HandleAsync(InviteStaffCommand command, CancellationToken ct = default)
    {
        if (command.Role is not (UserRole.MarinaOwner or UserRole.MarinaStaff))
            throw new InvalidOperationException("Only MarinaOwner and MarinaStaff roles can be assigned via staff invitation.");

        var existing = await userManager.FindByEmailAsync(command.Email);
        if (existing is not null)
            throw new InvalidOperationException($"A user with email '{command.Email}' already exists.");

        // Generate a temporary password. In production this would trigger an email invite.
        var temporaryPassword = $"Temp_{Guid.NewGuid():N}!A1";

        var user = new ApplicationUser
        {
            UserName = command.Email,
            Email = command.Email,
            FirstName = command.FirstName,
            LastName = command.LastName,
            Role = command.Role,
            TenantId = tenantContext.TenantId,
            MarinaId = command.MarinaId,
        };

        var result = await userManager.CreateAsync(user, temporaryPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create staff user: {errors}");
        }

        // Send invite email — non-fatal if delivery fails
        try
        {
            var marinaName = await db.Marinas
                .Where(m => m.Id == command.MarinaId)
                .Select(m => m.Name)
                .FirstOrDefaultAsync(ct) ?? "Your Marina";

            var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
            var encodedToken = Uri.EscapeDataString(token);
            var confirmationLink = $"{emailOptions.Value.AppBaseUrl}/confirm-email?userId={user.Id}&token={encodedToken}";
            await emailService.SendStaffInviteAsync(
                command.Email,
                $"{command.FirstName} {command.LastName}",
                marinaName,
                command.Role.ToString(),
                temporaryPassword,
                confirmationLink,
                ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send invite email to staff {Email}", command.Email);
        }

        return new InviteStaffResult(user.Id, temporaryPassword);
    }
}
