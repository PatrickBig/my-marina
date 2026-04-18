using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using MyMarina.Application.Profile;
using MyMarina.Infrastructure.Identity;
using System.Security.Claims;

namespace MyMarina.Infrastructure.Profile;

public class ChangeEmailCommandHandler(
    UserManager<ApplicationUser> userManager,
    IHttpContextAccessor httpContextAccessor) : IChangeEmailCommandHandler
{
    public async Task HandleAsync(ChangeEmailCommand command, CancellationToken ct = default)
    {
        var userId = GetUserId();
        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new KeyNotFoundException($"User {userId} not found.");

        if (!await userManager.CheckPasswordAsync(user, command.CurrentPassword))
            throw new InvalidOperationException("Current password is incorrect.");

        var existing = await userManager.FindByEmailAsync(command.NewEmail);
        if (existing is not null && existing.Id != userId)
            throw new EmailConflictException($"Email '{command.NewEmail}' is already in use.");

        var result = await userManager.SetEmailAsync(user, command.NewEmail);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));

        // Keep UserName in sync with Email
        await userManager.SetUserNameAsync(user, command.NewEmail);
    }

    private Guid GetUserId()
    {
        var sub = httpContextAccessor.HttpContext?.User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : throw new UnauthorizedAccessException();
    }
}

public sealed class EmailConflictException(string message) : Exception(message);
