## Why

Marina operators today have to set every slip's price one at a time. A marina with 300 slips means 300 manual edits whenever rates change — and rates change predictably (season turnover, annual lease renewals). There is no way to schedule a price change in advance, and no way to express "all 30-foot slips cost $X" as a single statement of intent. This makes large marinas painful to maintain and effectively blocks the platform from serving anyone above ~50 slips at scale.

This change replaces hand-edited per-slip prices with a **rules-first pricing model**: marinas define a small set of global rules (driven by slip dimensions, amenities, and listing kind), the engine resolves each slip's price by stacking matching rules, and operators only touch individual slips when they need to layer an adjustment on top (premium view, end-tie, extra power, etc.). Rules can also be scheduled to start on a future date, so next year's price increase can be staged today and will activate automatically.

## What Changes

- **NEW**: `PricingRule` entity owned by a marina — defines a base price for slips that match a set of criteria (size brackets, listing kind, lease term, amenity flags). Rules carry an `EffectiveFrom` and optional `EffectiveTo` so a marina can stage future rate changes.
- **NEW**: `SlipPriceAdjustment` entity owned by a slip — named additive adjustments (e.g., "+$200/yr deepwater", "+$5/ft/night endcap"). Adjustments stack on top of resolved rule prices. They are how operators handle the exceptions.
- **NEW**: Layered/stacking rule resolution — for a given slip + listing kind + as-of date, all rules whose predicates match contribute to the final price. Each rule declares its `ContributionKind` (`Base` or `Surcharge`) so the engine can pick exactly one base and add any number of surcharges. Slip-level adjustments are summed on top.
- **NEW**: Computed price view materialized on each slip (`ResolvedTransientBaseRate`, `ResolvedLeaseBaseRate`, `ResolvedAsOf`) so search, listing pages, and reservation pricing all read a single value without re-running the resolver on every query. Recomputed via background job (Hangfire) when rules or adjustments change, and when a scheduled rule crosses its effective date.
- **NEW**: Pricing rules UI under marina admin — list/create/edit/schedule rules, preview the resulting price for a sample slip, see which slips a rule will match.
- **NEW**: Slip detail UI gets an "Adjustments" panel for the additive overrides; the manual `DefaultTransientBaseRate` / `DefaultLeaseBaseRate` fields move to a read-only computed display.
- **BREAKING**: `Slip.DefaultTransientBaseRate` / `Slip.DefaultLeaseBaseRate` / `Slip.DefaultTransientRateKind` / `Slip.DefaultLeaseRateKind` / `Slip.DefaultTransientMinCharge` / `Slip.DefaultLeaseMinCharge` / `Slip.DefaultLeaseTerm` are removed as **writable** fields. The columns are renamed to the resolved fields above and become computed outputs of the rule engine. Any code path that sets these directly must instead create/modify a rule or adjustment.
- **BREAKING**: Slip lease renewal flow now looks up the price-as-of-renewal-date from the rule engine rather than copying the prior `SlipAssignment.BaseRate`. Existing active assignments keep their signed rate until they end (no retroactive change).
- **PROJECT DECISION**: Scheduled price changes do NOT modify in-flight `SlipAssignment.BaseRate`. The signed lease price is locked. Only renewals and new assignments pick up the new rule price.

## Capabilities

### New Capabilities
- `slip-pricing-rules`: Marina-owned rule engine — entity model, predicates (size brackets, amenity flags, lease term, listing kind), effective-from/effective-to scheduling, list/create/edit endpoints, and the deterministic stacking resolver that computes a slip's price from the matching rules.
- `slip-price-adjustments`: Slip-level additive overrides — entity model, CRUD endpoints, the rule for how adjustments combine with the resolved rule price (sum, with optional min-charge floor).
- `slip-resolved-pricing`: The cached resolved price on each slip (`ResolvedTransientBaseRate`, `ResolvedLeaseBaseRate`, `ResolvedAsOf`) — how it's recomputed (Hangfire job triggered by rule/adjustment changes plus a daily catch-up sweep that activates scheduled rules), the contract that every read path uses these fields instead of running the resolver inline, and the staleness budget.

### Modified Capabilities
- `slip-search`: The `priceMin` / `priceMax` filter and any per-night/per-period price returned in slip detail SHALL read from the resolved price fields. Search queries must NOT invoke the rule resolver per row. (The user-facing requirement that price filtering works is unchanged; the source of the price changes.)

## Impact

- **Domain**: New `PricingRule` and `SlipPriceAdjustment` entities. `Slip` loses six writable price fields and gains three computed ones. New `PricingRulePredicate` value object (range bounds + amenity flag set + lease term). New `ContributionKind` enum.
- **Application**: New `Marinas.PricingRules` module — command/query handlers for rule CRUD, the `PriceResolver` service. New `Slips.PriceAdjustments` module — handlers for adjustment CRUD. `UpdateSlipCommandHandler` loses its price-field writes.
- **Infrastructure**:
  - EF Core migration creates `PricingRules`, `SlipPriceAdjustments` tables; renames `Slip` price columns to their `Resolved*` form and drops the writable defaults columns; backfill seeds one rule per existing distinct (rate-kind, listing-kind, base-rate) combination so existing marinas don't lose data.
  - New Hangfire recurring job `PricingResolverJob` (runs hourly) that recomputes resolved prices for slips whose effective rule set is dirty or whose scheduled-rule boundary has just crossed.
  - `SlipSearchQueryHandler` and `MarinaRollupSearchQueryHandler` change their price predicates to read `ResolvedTransientBaseRate` / `ResolvedLeaseBaseRate`.
  - `CreateReservationCommandHandler` and `CreateLeaseInquiryCommandHandler` read the resolved price instead of `DefaultTransient*` / `DefaultLease*`.
  - `DemoSeedScript` updated to seed a representative set of pricing rules + a few slip-level adjustments (per CLAUDE.md "living artifact" rule).
- **API**: New `PricingRulesController` (marina-scoped CRUD + preview), new `SlipPriceAdjustmentsController` (slip-scoped CRUD). Existing slip update endpoint rejects writes to legacy price fields (returns 400 with a migration message). OpenAPI surface grows by ~10 routes.
- **Frontend** (`MyMarina.Web`): New marina admin route `/marinas/:id/pricing` for the rules UI (list, create, edit, schedule, preview). Slip detail page gains an "Adjustments" tab. Slip create/edit form loses the inline price inputs. `npm run generate-api` must run after backend changes land.
- **Demo / tier gating**: Add `Capabilities.ScheduledPricing` to `TierCapabilityRegistry`, assigned to `Pro`+ (Free tier can author rules but cannot schedule future-dated rules — the `EffectiveFrom > now` write returns 403).
- **Tests**: Unit tests for `PriceResolver` (rule stacking, scheduling boundaries, contribution kinds, predicate matching). Integration tests for the rule lifecycle including the renewal-pricing path. `DemoSeedScript` integration test continues to pass.
- **Performance / scale**: Resolver runs out-of-band; read paths stay single-table indexed reads. A 300-slip marina with 20 rules touches ≤6 000 rule-evaluations per recompute, well inside the hourly job budget.
