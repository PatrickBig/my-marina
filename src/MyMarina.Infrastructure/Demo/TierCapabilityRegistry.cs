using MyMarina.Domain.Enums;

namespace MyMarina.Infrastructure.Demo;

// Living document — update in the same PR as any new capability that's tier-gated.
// Use constants as attribute arguments: [RequiresTier(TierCapabilityRegistry.Tiers.AdvancedSearch)]
public static class TierCapabilityRegistry
{
    public static class Tiers
    {
        // Marketplace listing & transient bookings
        public const string MarketplaceListing   = nameof(MarketplaceListing);

        // Sublet flows (holder or owner listing while holder is away)
        public const string SubletFlows          = nameof(SubletFlows);

        // Full invoicing with line items + payment recording
        public const string Invoicing            = nameof(Invoicing);

        // Maintenance requests and work orders
        public const string Maintenance          = nameof(Maintenance);

        // Announcements
        public const string Announcements        = nameof(Announcements);

        // Staff invitations beyond the owner account
        public const string StaffInvitations     = nameof(StaffInvitations);

        // Revenue split configuration for sublet windows
        public const string RevenueSplits        = nameof(RevenueSplits);
    }

    // Maps each capability to the minimum tier required.
    public static readonly IReadOnlyDictionary<string, SubscriptionTier> MinimumTier =
        new Dictionary<string, SubscriptionTier>
        {
            [Tiers.MarketplaceListing]  = SubscriptionTier.Free,
            [Tiers.Invoicing]           = SubscriptionTier.Free,
            [Tiers.Maintenance]         = SubscriptionTier.Free,
            [Tiers.Announcements]       = SubscriptionTier.Free,
            [Tiers.StaffInvitations]    = SubscriptionTier.Pro,
            [Tiers.SubletFlows]         = SubscriptionTier.Pro,
            [Tiers.RevenueSplits]       = SubscriptionTier.Pro,
        };
}
