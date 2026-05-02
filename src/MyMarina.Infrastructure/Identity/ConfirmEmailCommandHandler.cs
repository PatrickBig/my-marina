using Microsoft.AspNetCore.Identity;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Identity;

namespace MyMarina.Infrastructure.Identity;

public class ConfirmEmailCommandHandler(UserManager<ApplicationUser> userManager)
    : ICommandHandler<ConfirmEmailCommand>
{
    public async Task HandleAsync(ConfirmEmailCommand command, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(command.UserId)
            ?? throw new InvalidOperationException("User not found.");

        var result = await userManager.ConfirmEmailAsync(user, command.Token);
        if (!result.Succeeded)
            throw new IdentityException(result.Errors);
    }
}
