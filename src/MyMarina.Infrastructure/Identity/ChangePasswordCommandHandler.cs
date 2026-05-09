using Microsoft.AspNetCore.Identity;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Identity;

namespace MyMarina.Infrastructure.Identity;

public class ChangePasswordCommandHandler(
    UserManager<ApplicationUser> userManager,
    IUserContext userContext)
    : ICommandHandler<ChangePasswordCommand>
{
    public async Task HandleAsync(ChangePasswordCommand command, CancellationToken ct = default)
    {
        if (!userContext.IsAuthenticated || userContext.UserId is null)
            throw new UnauthorizedAccessException();

        var user = await userManager.FindByIdAsync(userContext.UserId.Value.ToString())
            ?? throw new InvalidOperationException("User not found.");

        var result = await userManager.ChangePasswordAsync(user, command.CurrentPassword, command.NewPassword);
        if (!result.Succeeded)
            throw new IdentityException(result.Errors);
    }
}
