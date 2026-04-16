using Microsoft.AspNetCore.Identity;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Platform;
using MyMarina.Infrastructure.Identity;

namespace MyMarina.Infrastructure.Platform;

public class ResetUserPasswordCommandHandler(UserManager<ApplicationUser> userManager)
    : ICommandHandler<ResetUserPasswordCommand>
{
    public async Task HandleAsync(ResetUserPasswordCommand command, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(command.UserId.ToString())
            ?? throw new KeyNotFoundException($"User {command.UserId} not found.");

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, token, command.NewPassword);

        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Password reset failed: {errors}");
        }
    }
}
