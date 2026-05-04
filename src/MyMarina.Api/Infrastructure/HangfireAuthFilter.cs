using Hangfire.Dashboard;

namespace MyMarina.Api.Infrastructure;

public class HangfireAuthFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        if (httpContext.User.Identity?.IsAuthenticated != true)
            return false;

        return httpContext.User.IsInRole("PlatformOperator");
    }
}
