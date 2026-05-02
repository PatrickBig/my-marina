using Microsoft.AspNetCore.Identity;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Identity;

namespace MyMarina.Infrastructure.Identity;

public class ForgotPasswordCommandHandler(
    UserManager<ApplicationUser> userManager,
    IEmailService emailService)
    : ICommandHandler<ForgotPasswordCommand>
{
    public async Task HandleAsync(ForgotPasswordCommand command, CancellationToken ct = default)
    {
        var user = await userManager.FindByEmailAsync(command.Email);
        // Always return success — don't leak whether the email exists
        if (user is null || !user.EmailConfirmed) return;

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        await emailService.SendPasswordResetAsync(user.Email!, user.Id.ToString(), token, ct);
    }
}
