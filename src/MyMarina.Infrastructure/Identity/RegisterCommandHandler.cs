using Microsoft.AspNetCore.Identity;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Identity;

namespace MyMarina.Infrastructure.Identity;

public class RegisterCommandHandler(
    UserManager<ApplicationUser> userManager,
    IEmailService emailService)
    : ICommandHandler<RegisterCommand>
{
    public async Task HandleAsync(RegisterCommand command, CancellationToken ct = default)
    {
        if (!command.TermsAccepted)
            throw new InvalidOperationException("Terms of service must be accepted.");

        var user = new ApplicationUser
        {
            Id             = Guid.CreateVersion7(),
            UserName       = command.Email,
            Email          = command.Email,
            FirstName      = command.FirstName,
            LastName       = command.LastName,
            MarketingOptIn = command.MarketingOptIn,
            TermsAcceptedAt = DateTimeOffset.UtcNow,
        };

        var result = await userManager.CreateAsync(user, command.Password);
        if (!result.Succeeded)
            throw new IdentityException(result.Errors);

        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        await emailService.SendEmailConfirmationAsync(user.Email!, user.Id.ToString(), token, ct);
    }
}
