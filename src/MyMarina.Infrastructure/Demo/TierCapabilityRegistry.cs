// When a new feature ships, add a capability constant here and assign it to one or more tiers
// in the same PR. See CLAUDE.md for the standing rule.
using MyMarina.Domain.Enums;

namespace MyMarina.Infrastructure.Demo;

/// <summary>
/// Single source of truth for which named capabilities are available at each subscription tier.
/// Used by GET /demo/capabilities and the marketing site pricing table.
///
/// MAINTENANCE: This is a living document. When a new phase adds a feature, add a constant
/// and assign it to the appropriate tier(s). Free tier is intentionally very limited.
/// Exact Free/Pro/Enterprise boundaries are TBD pending a pricing discussion.
/// </summary>
public static class TierCapabilityRegistry
{
    // --- Capability name constants ---
    // TODO: pricing — assign each constant to the correct tier once pricing model is decided.

    public static class Capabilities
    {
        // Phase 1–4 capabilities (all currently available)
        public const string SlipManagement       = "slip-management";
        public const string BasicBookings        = "basic-bookings";
        public const string CustomerPortal       = "customer-portal";
        public const string InvoicingBasic       = "invoicing-basic";
        public const string BoatRegistration     = "boat-registration";
        public const string StaffAccounts        = "staff-accounts";

        // Phase 5 capabilities
        public const string Announcements        = "announcements";        // TODO: pricing
        public const string MaintenanceRequests  = "maintenance-requests"; // TODO: pricing
        public const string WorkOrders           = "work-orders";          // TODO: pricing

        // Multi-marina / advanced (likely Pro+)
        public const string MultiMarina         = "multi-marina";          // TODO: pricing
        public const string AdvancedReporting   = "advanced-reporting";    // TODO: pricing
        public const string ApiAccess           = "api-access";            // TODO: pricing
    }

    // --- Tier assignment maps ---
    // TODO: pricing — the assignments below are placeholders. Free is intentionally sparse.
    // Replace with final values after the pricing/feature-model discussion.

    private static readonly IReadOnlyList<string> FreeCapabilities =
    [
        Capabilities.SlipManagement,
        Capabilities.BasicBookings,
        // Free tier is deliberately limited — everything else requires Starter or above.
    ];

    private static readonly IReadOnlyList<string> StarterCapabilities =
    [
        ..FreeCapabilities,
        Capabilities.CustomerPortal,
        Capabilities.InvoicingBasic,
        Capabilities.BoatRegistration,
        Capabilities.StaffAccounts,
        Capabilities.Announcements,
        Capabilities.MaintenanceRequests,
    ];

    private static readonly IReadOnlyList<string> ProCapabilities =
    [
        ..StarterCapabilities,
        Capabilities.WorkOrders,
        Capabilities.MultiMarina,
        Capabilities.AdvancedReporting,
    ];

    private static readonly IReadOnlyList<string> EnterpriseCapabilities =
    [
        ..ProCapabilities,
        Capabilities.ApiAccess,
    ];

    public static IReadOnlyList<string> GetCapabilities(SubscriptionTier tier) => tier switch
    {
        SubscriptionTier.Free       => FreeCapabilities,
        SubscriptionTier.Starter    => StarterCapabilities,
        SubscriptionTier.Pro        => ProCapabilities,
        SubscriptionTier.Enterprise => EnterpriseCapabilities,
        _                           => FreeCapabilities,
    };

    public static bool HasCapability(SubscriptionTier tier, string capability)
        => GetCapabilities(tier).Contains(capability);
}
