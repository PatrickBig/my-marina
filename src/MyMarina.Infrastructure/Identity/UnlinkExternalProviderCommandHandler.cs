using Microsoft.AspNetCore.Identity;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Identity;

namespace MyMarina.Infrastructure.Identity;

public class UnlinkExternalProviderCommandHandler(UserManager<ApplicationUser> userManager)
    : ICommandHandler<UnlinkExternalProviderCommand>
{
    public async Task HandleAsync(UnlinkExternalProviderCommand command, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(command.UserId.ToString())
            ?? throw new InvalidOperationException("User not found.");

        var logins = await userManager.GetLoginsAsync(user);
        var target = logins.FirstOrDefault(l => l.LoginProvider.Equals(command.Provider, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"Provider '{command.Provider}' is not linked to this account.");

        if (logins.Count == 1 && !await userManager.HasPasswordAsync(user))
            throw new InvalidOperationException("Cannot unlink the only sign-in method. Add a password first.");

        var result = await userManager.RemoveLoginAsync(user, target.LoginProvider, target.ProviderKey);
        if (!result.Succeeded)
            throw new IdentityException(result.Errors);
    }
}
