using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using MyMarina.Application.Abstractions;
using MyMarina.Domain.Enums;

namespace MyMarina.Infrastructure.UserContext;

public class HttpUserContext : IUserContext
{
    public Guid? UserId { get; }
    public string? Email { get; }
    public bool IsAuthenticated { get; }
    public bool IsPlatformOperator { get; }
    public bool IsDemo { get; }
    public IReadOnlyList<MembershipClaim> Memberships { get; }
    public IReadOnlyList<BillingAccountMemberClaim> BillingAccounts { get; }

    public HttpUserContext(IHttpContextAccessor accessor)
    {
        var user = accessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            Memberships = [];
            BillingAccounts = [];
            return;
        }

        IsAuthenticated = true;

        var sub = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue("sub");
        if (Guid.TryParse(sub, out var userId))
            UserId = userId;

        Email = user.FindFirstValue(ClaimTypes.Email)
            ?? user.FindFirstValue("email");

        IsPlatformOperator = user.FindFirstValue("platform_role") == "Operator";
        IsDemo = user.FindFirstValue("is_demo") == "true";

        Memberships = ParseMemberships(user.FindFirstValue("memberships"));
        BillingAccounts = ParseBillingAccounts(user.FindFirstValue("billing_accounts"));
    }

    public bool HasMarinaAccess(Guid marinaId, MembershipRole minimumRole = MembershipRole.Staff)
    {
        if (IsPlatformOperator) return true;
        return Memberships.Any(m =>
            m.Role >= minimumRole &&
            (m.Scope == MembershipScope.Marina && m.MarinaId == marinaId ||
             m.Scope == MembershipScope.Tenant));
    }

    public bool HasTenantAccess(Guid tenantId, MembershipRole minimumRole = MembershipRole.Staff)
    {
        if (IsPlatformOperator) return true;
        return Memberships.Any(m =>
            m.Role >= minimumRole &&
            m.TenantId == tenantId);
    }

    public bool HasBillingAccountAccess(Guid billingAccountId)
    {
        if (IsPlatformOperator) return true;
        return BillingAccounts.Any(b => b.BillingAccountId == billingAccountId);
    }

    private static IReadOnlyList<MembershipClaim> ParseMemberships(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            var items = JsonSerializer.Deserialize<JsonElement[]>(json);
            if (items is null) return [];
            var result = new List<MembershipClaim>();
            foreach (var item in items)
            {
                var scope = Enum.TryParse<MembershipScope>(
                    item.GetProperty("scope").GetString(), out var s) ? s : MembershipScope.Marina;
                var tenantId = Guid.TryParse(
                    item.GetProperty("tenant_id").GetString(), out var tid) ? tid : Guid.Empty;
                Guid? marinaId = null;
                if (item.TryGetProperty("marina_id", out var mid) && mid.ValueKind != JsonValueKind.Null)
                    marinaId = Guid.TryParse(mid.GetString(), out var m) ? m : null;
                var role = Enum.TryParse<MembershipRole>(
                    item.GetProperty("role").GetString(), out var r) ? r : MembershipRole.Staff;
                string? tier = item.TryGetProperty("tier", out var t) ? t.GetString() : null;
                result.Add(new MembershipClaim(scope, tenantId, marinaId, role, tier));
            }
            return result;
        }
        catch { return []; }
    }

    private static IReadOnlyList<BillingAccountMemberClaim> ParseBillingAccounts(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            var items = JsonSerializer.Deserialize<JsonElement[]>(json);
            if (items is null) return [];
            var result = new List<BillingAccountMemberClaim>();
            foreach (var item in items)
            {
                var billingAccountId = Guid.TryParse(
                    item.GetProperty("billing_account_id").GetString(), out var bid) ? bid : Guid.Empty;
                var marinaId = Guid.TryParse(
                    item.GetProperty("marina_id").GetString(), out var mid) ? mid : Guid.Empty;
                var role = Enum.TryParse<BillingAccountRole>(
                    item.GetProperty("role").GetString(), out var r) ? r : BillingAccountRole.Member;
                result.Add(new BillingAccountMemberClaim(billingAccountId, marinaId, role));
            }
            return result;
        }
        catch { return []; }
    }
}
