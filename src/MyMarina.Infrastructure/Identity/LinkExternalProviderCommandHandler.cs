using Microsoft.AspNetCore.Identity;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Identity;

namespace MyMarina.Infrastructure.Identity;

public class LinkExternalProviderCommandHandler(UserManager<ApplicationUser> userManager)
    : ICommandHandler<LinkExternalProviderCommand>
{
    public async Task HandleAsync(LinkExternalProviderCommand command, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(command.UserId.ToString())
            ?? throw new InvalidOperationException("User not found.");

        var logins = await userManager.GetLoginsAsync(user);
        if (logins.Any(l => l.LoginProvider.Equals(command.Provider, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Provider '{command.Provider}' is already linked to this account.");

        var loginInfo = new UserLoginInfo(command.Provider, command.ProviderKey, command.Provider);
        var result = await userManager.AddLoginAsync(user, loginInfo);
        if (!result.Succeeded)
            throw new IdentityException(result.Errors);
    }
}
