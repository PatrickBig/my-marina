using Microsoft.AspNetCore.Identity;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Staff;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Identity;

namespace MyMarina.Infrastructure.Staff;

public class InviteStaffCommandHandler(
    UserManager<ApplicationUser> userManager,
    ITenantContext tenantContext) : ICommandHandler<InviteStaffCommand, InviteStaffResult>
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

        return new InviteStaffResult(user.Id, temporaryPassword);
    }
}
