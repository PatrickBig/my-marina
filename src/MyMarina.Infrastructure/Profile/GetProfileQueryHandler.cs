using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using MyMarina.Application.Profile;
using MyMarina.Infrastructure.Identity;
using System.Security.Claims;

namespace MyMarina.Infrastructure.Profile;

public class GetProfileQueryHandler(
    UserManager<ApplicationUser> userManager,
    IHttpContextAccessor httpContextAccessor) : IGetProfileQueryHandler
{
    public async Task<GetProfileResult> HandleAsync(GetProfileQuery query, CancellationToken ct = default)
    {
        var userId = GetUserId();
        var user = await userManager.FindByIdAsync(userId.ToString())
            ?? throw new KeyNotFoundException($"User {userId} not found.");

        return new GetProfileResult(user.FirstName, user.LastName, user.Email!, user.PhoneNumber);
    }

    private Guid GetUserId()
    {
        var sub = httpContextAccessor.HttpContext?.User.FindFirstValue("sub");
        return Guid.TryParse(sub, out var id) ? id : throw new UnauthorizedAccessException();
    }
}
