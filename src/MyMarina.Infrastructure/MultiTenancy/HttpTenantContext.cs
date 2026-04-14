using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using MyMarina.Application.Abstractions;
using MyMarina.Domain.Enums;

namespace MyMarina.Infrastructure.MultiTenancy;

/// <summary>
/// Resolves the current tenant and marina from the authenticated user's JWT claims.
/// Registered as scoped — one instance per HTTP request.
/// </summary>
public class HttpTenantContext : ITenantContext, IMarinaContext
{
    public Guid TenantId { get; }
    public bool IsPlatformOperator { get; }
    public Guid? MarinaId { get; }

    public HttpTenantContext(IHttpContextAccessor accessor)
    {
        var user = accessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
            return;

        var roleStr = user.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        IsPlatformOperator = roleStr == nameof(UserRole.PlatformOperator);

        if (!IsPlatformOperator)
        {
            var tenantClaim = user.FindFirstValue("tenant_id");
            if (Guid.TryParse(tenantClaim, out var tenantId))
                TenantId = tenantId;

            var marinaClaim = user.FindFirstValue("marina_id");
            if (Guid.TryParse(marinaClaim, out var marinaId))
                MarinaId = marinaId;
        }
    }
}
