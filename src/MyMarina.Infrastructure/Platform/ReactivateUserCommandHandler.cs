using Microsoft.AspNetCore.Identity;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Platform;
using MyMarina.Infrastructure.Identity;

namespace MyMarina.Infrastructure.Platform;

public class ReactivateUserCommandHandler(UserManager<ApplicationUser> userManager)
    : ICommandHandler<ReactivateUserCommand>
{
    public async Task HandleAsync(ReactivateUserCommand command, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(command.UserId.ToString())
            ?? throw new KeyNotFoundException($"User {command.UserId} not found.");

        user.IsActive = true;
        var result = await userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"User reactivation failed: {errors}");
        }
    }
}
