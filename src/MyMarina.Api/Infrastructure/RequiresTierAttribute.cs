using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using MyMarina.Domain.Enums;

namespace MyMarina.Api.Infrastructure;

/// <summary>
/// Rejects requests whose JWT subscription_tier claim is below the required tier.
/// Returns 403 with error_code "tier_required" so clients can show an upgrade prompt.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequiresTierAttribute(SubscriptionTier minimumTier) : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var tierClaim = context.HttpContext.User.FindFirst("subscription_tier")?.Value;

        if (!Enum.TryParse<SubscriptionTier>(tierClaim, ignoreCase: true, out var tenantTier))
            tenantTier = SubscriptionTier.Free;

        if (tenantTier < minimumTier)
        {
            context.Result = new ObjectResult(new
            {
                error_code = "tier_required",
                required_tier = minimumTier.ToString(),
                current_tier = tenantTier.ToString(),
            })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}
