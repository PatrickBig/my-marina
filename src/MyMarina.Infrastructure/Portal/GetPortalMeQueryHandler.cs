using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Portal;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Portal;

public class GetPortalMeQueryHandler(
    AppDbContext db,
    ICustomerContext customerContext,
    IHttpContextAccessor accessor) : IQueryHandler<GetPortalMeQuery, PortalMeDto?>
{
    public async Task<PortalMeDto?> HandleAsync(GetPortalMeQuery query, CancellationToken ct = default)
    {
        var userIdStr = accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? accessor.HttpContext?.User.FindFirstValue("sub");
        if (!Guid.TryParse(userIdStr, out var userId))
            return null;

        var firstName = accessor.HttpContext?.User.FindFirstValue(ClaimTypes.GivenName)
                        ?? accessor.HttpContext?.User.FindFirstValue("given_name") ?? "";
        var lastName  = accessor.HttpContext?.User.FindFirstValue(ClaimTypes.Surname)
                        ?? accessor.HttpContext?.User.FindFirstValue("family_name") ?? "";
        var email     = accessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email)
                        ?? accessor.HttpContext?.User.FindFirstValue("email") ?? "";

        var account = await db.CustomerAccounts
            .Where(a => a.Id == customerContext.CustomerAccountId)
            .Select(a => new { a.DisplayName, a.BillingEmail, a.BillingPhone })
            .FirstOrDefaultAsync(ct);

        if (account is null) return null;

        return new PortalMeDto(
            UserId: userId,
            Email: email,
            FirstName: firstName,
            LastName: lastName,
            CustomerAccountId: customerContext.CustomerAccountId,
            AccountDisplayName: account.DisplayName,
            BillingEmail: account.BillingEmail,
            BillingPhone: account.BillingPhone);
    }
}
