using Microsoft.AspNetCore.Identity;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Identity;

namespace MyMarina.Infrastructure.Identity;

public class UpdateProfileCommandHandler(
    UserManager<ApplicationUser> userManager,
    IUserContext userContext)
    : ICommandHandler<UpdateProfileCommand>
{
    public async Task HandleAsync(UpdateProfileCommand command, CancellationToken ct = default)
    {
        if (!userContext.IsAuthenticated || userContext.UserId is null)
            throw new UnauthorizedAccessException();

        var user = await userManager.FindByIdAsync(userContext.UserId.Value.ToString())
            ?? throw new InvalidOperationException("User not found.");

        if (command.FirstName is not null) user.FirstName = command.FirstName;
        if (command.LastName is not null) user.LastName = command.LastName;
        if (command.PhoneNumber is not null) user.PhoneNumber = command.PhoneNumber;
        if (command.MarketingOptIn is not null) user.MarketingOptIn = command.MarketingOptIn.Value;

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
            throw new IdentityException(result.Errors);
    }
}
