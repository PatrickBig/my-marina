using Microsoft.AspNetCore.Identity;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Identity;

namespace MyMarina.Infrastructure.Identity;

public class MeQueryHandler(
    UserManager<ApplicationUser> userManager,
    IUserContext userContext)
    : IQueryHandler<MeQuery, MeResponse>
{
    public async Task<MeResponse> HandleAsync(MeQuery query, CancellationToken ct = default)
    {
        if (!userContext.IsAuthenticated || userContext.UserId is null)
            throw new UnauthorizedAccessException();

        var user = await userManager.FindByIdAsync(userContext.UserId.Value.ToString())
            ?? throw new InvalidOperationException("User not found.");

        return new MeResponse(
            Id: user.Id,
            Email: user.Email!,
            EmailConfirmed: user.EmailConfirmed,
            FirstName: user.FirstName,
            LastName: user.LastName,
            PhoneNumber: user.PhoneNumber,
            ProfilePhotoUrl: user.ProfilePhotoUrl,
            MarketingOptIn: user.MarketingOptIn,
            CreatedAt: user.CreatedAt,
            LastLoginAt: user.LastLoginAt,
            Memberships: userContext.Memberships,
            BillingAccounts: userContext.BillingAccounts
        );
    }
}
