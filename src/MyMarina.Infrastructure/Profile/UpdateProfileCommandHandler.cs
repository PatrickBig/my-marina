using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using MyMarina.Application.Profile;
using MyMarina.Infrastructure.Identity;
using System.Security.Claims;

namespace MyMarina.Infrastructure.Profile;

public class UpdateProfileCommandHandler(
    UserManager<ApplicationUser> userManager,
    IHttpContextAccessor httpContextAccessor) : IUpdateProfileCommandHandler
{
    public async Task HandleAsync(UpdateProfileCommand command, CancellationToken ct = default)
    {
        var userId = GetUserId();
        if (string.IsNullOrWhiteSpace(command.FirstName))
            throw new ArgumentException("First name is required.");
        if (string.IsNullOrWhiteSpace(command.LastName))
            throw new ArgumentException("Last name is required.");

        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new KeyNotFoundException($"User {userId} not found.");

        user.FirstName = command.FirstName;
        user.LastName = command.LastName;
        user.PhoneNumber = command.PhoneNumber;

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join("; ", result.Errors.Select(e => e.Description)));
    }

    private Guid GetUserId()
    {
        var sub = httpContextAccessor.HttpContext?.User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : throw new UnauthorizedAccessException();
    }
}
