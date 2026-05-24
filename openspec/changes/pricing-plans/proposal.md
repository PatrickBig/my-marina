## Why

The current pricing system uses a predicate-based rule engine where rules match slips by attribute filters (size bounds, amenity flags, listing kind). While flexible, it is unintuitive for marina operators: there is no clear answer to "what does slip A-7 cost?" without tracing predicate logic. The system is being replaced before any production use, making this the right moment for a clean redesign.

## What Changes

- **BREAKING** Remove `PricingRule` entity, table, CRUD endpoints, and predicate resolution engine entirely
- **BREAKING** Remove `SlipPriceAdjustment` entity, table, and CRUD endpoints entirely
- **BREAKING** Remove `PricingResolverSweepJob`, `PricingResolverJob`, and `PricingResolverSweepState`
- **BREAKING** Remove `ResolvedTransientBaseRate`, `ResolvedLeaseBaseRate`, `ResolvedTransientRateKind`, `ResolvedLeaseRateKind`, `ResolvedAsOf` columns from `Slip`
- **BREAKING** Reset all EF Core migrations to a single `InitialSchema` migration (app is pre-production)
- Add `PricingPlan` entity: a named bundle containing transient rates, lease rates, and flat amenity add-on amounts per listing kind
- Add `Slip.PricingPlanId` (nullable FK): null means "use marina's active default plan"
- Add `RateKind.PerArea` (W × L) alongside existing `Flat` and `PerFoot`
- Add a `IsDefault` flag on `PricingPlan`: marina has exactly one active default at any time; promotion/demotion is explicit
- Re-add `ResolvedTransientBaseRate` and `ResolvedLeaseBaseRate` to `Slip` (cached for search performance), recomputed event-driven when a plan's rates change or a slip's plan assignment changes
- Add bulk-assign endpoint and UI: filter slips by dock, length range, beam range, amenities, current plan → assign all matching slips to a chosen plan in one operation
- Add marina dashboard compliance check: if no default plan exists, show a prominent warning and block marketplace listing
- Add optional "Default pricing plan" step to the marina setup wizard (between slip preview and photos); skippable, but publish step disables the listing toggle if no plan exists
- Update demo seed data with representative pricing plans

## Capabilities

### New Capabilities

- `pricing-plans`: Marina-scoped pricing plan entity with transient/lease rates, amenity add-ons, default promotion, and slip assignment
- `slip-plan-assignment`: Per-slip plan assignment (nullable FK), bulk-assign by filter, and event-driven resolved-price cache recomputation

### Modified Capabilities

- `slip-search`: Price-range filtering switches from `Slip.ResolvedTransientBaseRate` (computed by old resolver) to the same cached columns, now populated by plan-based resolution; no API contract change for search callers
- `slip-pricing-rules`: Entire spec superseded — requirements replaced by `pricing-plans`; old spec retained in archive for reference only
- `slip-price-adjustments`: Entire spec superseded — capability removed; per-slip variation handled by assigning a dedicated plan

## Impact

- **Removed controllers**: `PricingRulesController`, `SlipPriceAdjustmentsController`
- **Removed frontend pages/components**: `PricingRulesPage.tsx`, `SlipAdjustmentsTab.tsx`, `PreviewPriceCard.tsx`, `src/api/queries/pricing.ts`
- **New controller**: `PricingPlansController` under `/marinas/{marinaId}/pricing/plans`
- **New frontend page**: `PricingPlansPage.tsx` replacing `PricingRulesPage.tsx`
- **Schema**: All migrations collapsed to a new `InitialSchema`; dev database drop-and-recreate required
- **Search**: `SlipAvailabilityFilter`, `MarinaRollupSearchQueryHandler` continue using `Slip.ResolvedTransientBaseRate` / `ResolvedLeaseBaseRate` — no change to search API callers
- **Demo seed**: `DemoSeedScript` updated to seed pricing plans instead of pricing rules
