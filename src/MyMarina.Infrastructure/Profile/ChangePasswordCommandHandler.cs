using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using MyMarina.Application.Profile;
using MyMarina.Infrastructure.Identity;
using System.Security.Claims;

namespace MyMarina.Infrastructure.Profile;

public class ChangePasswordCommandHandler(
    UserManager<ApplicationUser> userManager,
    IHttpContextAccessor httpContextAccessor) : IChangePasswordCommandHandler
{
    public async Task HandleAsync(ChangePasswordCommand command, CancellationToken ct = default)
    {
        var userId = GetUserId();
        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new KeyNotFoundException($"User {userId} not found.");

        if (command.NewPassword == command.CurrentPassword)
            throw new PasswordChangeFailedException(["New password must differ from current password."]);

        var result = await userManager.ChangePasswordAsync(user, command.CurrentPassword, command.NewPassword);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => e.Description).ToList();
            throw new PasswordChangeFailedException(errors);
        }
    }

    private Guid GetUserId()
    {
        var sub = httpContextAccessor.HttpContext?.User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : throw new UnauthorizedAccessException();
    }
}

public sealed class PasswordChangeFailedException(IReadOnlyList<string> errors)
    : Exception(string.Join("; ", errors))
{
    public IReadOnlyList<string> Errors { get; } = errors;
}
