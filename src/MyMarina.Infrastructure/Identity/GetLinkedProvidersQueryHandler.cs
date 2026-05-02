using Microsoft.AspNetCore.Identity;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Identity;

namespace MyMarina.Infrastructure.Identity;

public class GetLinkedProvidersQueryHandler(UserManager<ApplicationUser> userManager)
    : IQueryHandler<GetLinkedProvidersQuery, IReadOnlyList<LinkedProviderDto>>
{
    public async Task<IReadOnlyList<LinkedProviderDto>> HandleAsync(
        GetLinkedProvidersQuery query,
        CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(query.UserId.ToString())
            ?? throw new InvalidOperationException("User not found.");

        var logins = await userManager.GetLoginsAsync(user);
        return logins.Select(l => new LinkedProviderDto(l.LoginProvider)).ToList();
    }
}
