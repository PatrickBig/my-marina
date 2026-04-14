using Hangfire.Dashboard;
using MyMarina.Domain.Enums;

namespace MyMarina.Api.Infrastructure;

/// <summary>
/// Restricts the Hangfire dashboard to authenticated platform operators.
/// </summary>
public class HangfireAuthFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        if (!httpContext.User.Identity?.IsAuthenticated == true)
            return false;

        return httpContext.User.IsInRole(nameof(UserRole.PlatformOperator));
    }
}
