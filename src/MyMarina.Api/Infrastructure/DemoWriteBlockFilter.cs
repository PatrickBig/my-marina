using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MyMarina.Application.Abstractions;

namespace MyMarina.Api.Infrastructure;

// Registered as a global filter. Blocks all state-mutating requests when the
// caller is in a demo session, while leaving reads and auth endpoints untouched.
public class DemoWriteBlockFilter(IUserContext userContext) : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        if (!userContext.IsDemo) return;

        var method = context.HttpContext.Request.Method;

        if (HttpMethods.IsGet(method) ||
            HttpMethods.IsHead(method) ||
            HttpMethods.IsOptions(method))
            return;

        // Allow auth endpoints so the demo session stays functional.
        var path = context.HttpContext.Request.Path.Value ?? string.Empty;
        if (path.StartsWith("/auth/", StringComparison.OrdinalIgnoreCase))
            return;

        context.Result = new ObjectResult(new ProblemDetails
        {
            Title  = "Demo mode — read-only",
            Detail = "This is a live demo. Sign up at mymarina.org to make changes.",
            Status = StatusCodes.Status403Forbidden,
            Type   = "https://mymarina.org/errors/demo-read-only",
        })
        { StatusCode = StatusCodes.Status403Forbidden };
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
