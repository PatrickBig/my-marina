using Microsoft.AspNetCore.Identity;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Identity;

namespace MyMarina.Infrastructure.Identity;

public class ResendConfirmationCommandHandler(
    UserManager<ApplicationUser> userManager,
    IEmailService emailService)
    : ICommandHandler<ResendConfirmationCommand>
{
    public async Task HandleAsync(ResendConfirmationCommand command, CancellationToken ct = default)
    {
        var user = await userManager.FindByEmailAsync(command.Email);
        if (user is null || user.EmailConfirmed) return;

        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        await emailService.SendEmailConfirmationAsync(user.Email!, user.Id.ToString(), token, ct);
    }
}
