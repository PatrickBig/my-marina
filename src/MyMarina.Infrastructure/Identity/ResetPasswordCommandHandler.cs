using Microsoft.AspNetCore.Identity;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Identity;

namespace MyMarina.Infrastructure.Identity;

public class ResetPasswordCommandHandler(UserManager<ApplicationUser> userManager)
    : ICommandHandler<ResetPasswordCommand>
{
    public async Task HandleAsync(ResetPasswordCommand command, CancellationToken ct = default)
    {
        var user = await userManager.FindByEmailAsync(command.Email)
            ?? throw new InvalidOperationException("User not found.");

        var result = await userManager.ResetPasswordAsync(user, command.Token, command.NewPassword);
        if (!result.Succeeded)
            throw new IdentityException(result.Errors);
    }
}
