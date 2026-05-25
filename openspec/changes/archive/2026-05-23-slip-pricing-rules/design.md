## Context

The current pricing model stores a base rate directly on each `Slip` row (`DefaultTransientBaseRate`, `DefaultLeaseBaseRate`, plus their `RateKind` and `MinCharge` siblings). A marina operator must touch every slip individually to change rates, and there is no notion of "the price as of a future date." The marketplace search filter, the slip detail page, the reservation pricing path, and the lease inquiry handler all read these per-slip columns directly.

The boundary we're crossing in this change is from **stored prices** to **derived prices**. Prices become an output of a rule engine, not an input. The trade-off is that the resolver has to run somewhere — and search must remain a single-table indexed query under load — so we precompute and cache the result on the `Slip` row itself.

Pricing touches many subsystems (search, reservations, leases, demo seed, tier gating, frontend admin), the data model changes are non-trivial, and there is a real backfill story for existing rows. This warrants a full design doc rather than going straight to tasks.

Stakeholders: marina operators (admin UX), boaters (price filter accuracy), platform operators (tier gating + demo seed integrity), and the next phase of engineering (resolver must remain understandable so future predicates can be added without rewriting it).

## Goals / Non-Goals

**Goals:**
- Marina operators can define a small number of global rules that produce the right base price for the vast majority of their slips, while keeping a surgical "adjustments" escape hatch for individual exceptions.
- Operators can schedule a rate change for a future date and walk away — no manual activation step on the effective date.
- Existing signed leases keep the price they signed at. Renewals and new assignments pick up the current resolved price.
- Search and listing reads stay O(1) per slip (no per-row resolver execution).
- The resolver is deterministic: given the same rules + adjustments + as-of date, the output never changes.
- Rule resolution rules are explicit enough that an operator can hover a slip and see exactly which rules contributed and what each adjustment added.

**Non-Goals:**
- Dynamic / demand-based pricing (surge, occupancy-driven). This change is rules + schedule, not a yield-management engine.
- Per-customer pricing or coupon-style discounts. Discounts on `AvailabilityWindow` (weekly/monthly) stay where they are.
- Cross-marina pricing (a chain operating multiple marinas with shared rules). Each marina owns its own rules.
- Re-pricing in-flight `SlipAssignment` records. Signed leases are immutable from the engine's perspective.
- Tax / fee modeling. This change resolves a **base rate**; downstream invoice line items continue to handle taxes and additional fees.
- A "diff preview" that simulates pending future rules against historical reservations. The preview helper only shows current and same-day resolved prices.

## Decisions

### Decision 1: Layered/stacking resolution with `ContributionKind`

Each `PricingRule` declares a `ContributionKind`: `Base` or `Surcharge`.

- For a given (slip, listingKind, asOf) tuple, the resolver finds every rule whose predicate matches AND whose effective window covers `asOf`.
- Among matching `Base` rules, the **highest-priority** one wins (priority is an explicit `int` on the rule; ties broken by `CreatedAt` ascending for determinism). Exactly one base contributes.
- ALL matching `Surcharge` rules contribute additively.
- Finally, all active `SlipPriceAdjustment` rows for that slip and listing kind are summed on top.
- A `MinCharge` floor (if set on the base rule) is applied last.

**Rationale**: The user asked for "layered/stacking" and called out that amenities feel like surcharges layered on a size-driven base. A single `ContributionKind` flag captures that cleanly and keeps the mental model "exactly one base, any number of surcharges, any number of slip overrides." Alternatives considered: a free-for-all stack (no base/surcharge distinction) — rejected because it makes "what determines the floor price for a 30-ft slip?" ambiguous; a strict bracket model — rejected because the user wants amenity-driven surcharges, which don't fit cleanly into size brackets.

### Decision 2: Predicate is a flat value object, not an expression tree

`PricingRulePredicate` is a flat record:

```csharp
public record PricingRulePredicate(
    ListingKind ListingKind,         // Transient or Lease — required
    LeaseTerm? LeaseTerm,            // required when ListingKind = Lease
    decimal? MinLength, decimal? MaxLength,
    decimal? MinBeam,   decimal? MaxBeam,
    bool? RequiresElectric,
    bool? RequiresWater,
    bool? RequiresPumpOut,
    bool? RequiresCovered,
    bool? RequiresIndoor);
```

A null bound means "no constraint on this dimension." A null bool means "don't filter on this." Matching is straightforward AND-of-clauses.

**Rationale**: Predicates need to be queryable (we'll need "show me which slips this rule will affect" in the UI), serializable for audit/history, and easy to reason about in tests. A flat value object hits all three. An expression tree would let operators write arbitrary boolean combinations but adds a parser/evaluator and a UI builder we don't need for v1. We can add more fields without breaking existing rules.

### Decision 3: Cache the resolved price on the `Slip` row

Three new computed columns on `Slip`: `ResolvedTransientBaseRate decimal?`, `ResolvedLeaseBaseRate decimal?`, `ResolvedAsOf timestamptz` (the date the resolver evaluated against; usually `now()`). A slip with no matching base rule resolves to `null` (meaning "not listed").

Search, listing pages, slip detail, and reservation/lease pricing all read these columns. They are never written by an interactive command handler — only by the resolver job.

**Rationale**: Search runs on bounding-box queries that hit hundreds of slips at once. Running the resolver per-row in SQL is possible but couples the engine to the database dialect and complicates the predicate set. Running it in-memory per request is unacceptable for the 300-slip case. Materializing the result on the row keeps every read path a single indexed scan, just like today.

**Alternative considered**: A separate `SlipResolvedPrice` table. Rejected — adds a join to every search query and provides no real benefit since the relationship is 1:1 with `Slip` and the columns are tiny.

### Decision 4: Recompute via Hangfire background job + targeted invalidation

Two triggers fire the resolver:

1. **Targeted**: When a `PricingRule` or `SlipPriceAdjustment` is created/edited/deleted, an `EnqueueResolveSlipPricesCommand` enqueues a Hangfire job. The job recomputes only the slips matched by that rule's predicate (or the one slip touched by an adjustment).
2. **Sweep**: A recurring Hangfire job (`PricingResolverSweepJob`) runs hourly. It finds rules whose `EffectiveFrom` or `EffectiveTo` boundary crossed since the last sweep and recomputes affected slips. This is what makes scheduled price changes activate without manual intervention.

The targeted path keeps the interactive write fast and bounded. The sweep is the safety net for time-based activation.

**Rationale**: Doing the recompute synchronously in the command handler would block API responses (a "raise prices on all 30-ft slips" rule could touch 100 slips). Hangfire is already in the stack and already handles retries, idempotency keys, and visibility. We accept a few-seconds lag between rule edit and search reflecting it — acceptable for marina admin workflows.

**Alternative considered**: PostgreSQL `pg_cron` or a materialized view. Rejected — adds infrastructure surface area, and the predicate complexity is fine in C#.

### Decision 5: Renewal pricing reads the rule engine; signed leases are locked

`SlipAssignment.BaseRate` is **set at creation** and never modified by the pricing engine. When an assignment renews (either manual or automatic), the renewal handler calls `PriceResolver.Resolve(slipId, listingKind, asOf: renewalDate)` and uses that as the new assignment's `BaseRate`.

This is what makes the user's "$3200 this year, $3300 next year" example work: the operator stages a new lease rule with `EffectiveFrom = 2027-01-01`, the existing assignment ends 2026-12-31, the renewal command resolves the price using the rule that's effective on 2027-01-01.

**Rationale**: Legal/contractual — a signed lease price doesn't change because the marina edited a rule. This is also why we don't expose a "recalculate all active assignments" admin action. Operators wanting to bump everyone mid-lease must amend the lease, which is out of scope.

### Decision 6: Backfill existing slip prices into a "legacy" rule per marina

Migration creates, for each marina that currently has any slip with a non-null `DefaultTransientBaseRate` or `DefaultLeaseBaseRate`, an auto-generated `PricingRule` named `"Legacy import — <date>"` at priority `-1000` (below anything an operator would create) with a predicate matching by exact (rate kind, listing kind, base rate, lease term, min charge). For slips whose price is identical to that rule's output, no adjustment is needed. For slips whose price differs from any rule due to ad-hoc edits, a `SlipPriceAdjustment` is created carrying the delta.

This guarantees that no existing slip's resolved price changes on the day this migration runs.

**Rationale**: A clean cutover is the only acceptable answer for a multi-tenant system that's about to ship to real marinas. Operators can audit and consolidate the legacy rules later; the system stays internally consistent immediately.

### Decision 7: Free tier can create rules but not schedule future-dated ones

`PricingRule.EffectiveFrom > now()` requires `Tenant.SubscriptionTier >= Pro`. Enforced via `[RequiresTier(Pro)]` on the create/update endpoint when `EffectiveFrom` is in the future. Free tier marinas can still use rules — they just can't pre-stage them.

**Rationale**: Scheduled pricing is the feature that justifies an upsell to operators. Rules + adjustments by themselves are big enough usability wins that they should be available on Free; the time dimension is the paid layer.

## Risks / Trade-offs

- **Resolver lag** → Hourly sweep means a scheduled rule may take up to ~60 minutes to activate after its `EffectiveFrom`. Mitigation: marina admin UI shows "next recompute in N minutes" near scheduled rules; targeted triggers fire immediately for edits.
- **Backfill miscategorization** → Auto-generated legacy rules may not match how the operator would have organized rules themselves, leading to a cluttered rule list. Mitigation: legacy rules are tagged in the UI with a banner offering "review and consolidate" guidance; they sort last by default.
- **Stacking surprises** → Operators may not anticipate the total when two surcharge rules both match. Mitigation: the rule list UI ships with a "test against this slip" preview that shows every contributing rule and the final number side by side.
- **Predicate evolution** → Adding a new predicate field later (e.g., `dock zone`) means migrating the value object. Mitigation: predicates serialize to JSONB so adding nullable fields is non-breaking; existing rules read as "no constraint" on the new field.
- **Adjustment drift over time** → Operators may accumulate dozens of one-off slip adjustments instead of evolving rules. Mitigation: out of scope for v1 — this is a UX hygiene concern, not a correctness concern. We can ship a "promote adjustment to rule" workflow later.
- **Renewal-time price spike surprise to boater** → A boater whose lease renews into a higher price under a scheduled rule needs to be warned. Mitigation: the renewal email already exists; we add the new resolved price + delta to the email template (small extra task in this change).
- **Migration size** → 1M+ users plus a marina's full slip catalog means the legacy-import migration is the largest single write. Mitigation: backfill runs in batches of 5 000 slips per transaction; deferred to a separate migration that's safe to run independently from the schema migration.
- **Demo-tenant drift** → `DemoSeedScript` becomes more complex and slower. Mitigation: keep the rule set small but representative (~5 rules + ~10 adjustments), assert in the existing integration test that the resolver produces non-null prices on every demo slip.

## Migration Plan

1. **Schema migration (`Phase19_PricingRules`)**: Add `PricingRules` and `SlipPriceAdjustments` tables. Add the three `Resolved*` columns to `Slip`. Index `(MarinaId, EffectiveFrom, EffectiveTo)` on rules. Index `(SlipId, ListingKind)` on adjustments. Keep the legacy `DefaultTransient*` / `DefaultLease*` columns for now to avoid an unsafe drop.
2. **Backfill migration (`Phase19_PricingRulesBackfill`)**: Generate the legacy rules and adjustments per Decision 6. Populate `Resolved*` columns. Runs in batched transactions, idempotent.
3. **Code switchover**: Search, slip detail, reservation/lease pricing read from `Resolved*` columns. Slip update endpoint rejects writes to legacy fields. Hangfire jobs registered.
4. **Frontend ships behind the same release**: New pricing admin UI live, slip edit form's price inputs removed.
5. **Cleanup migration (`Phase19_PricingRulesDropLegacy`)**: After one release cycle in staging confirms no code path writes to the legacy columns, drop them. (CLAUDE.md notes destructive changes are acceptable pre-prod; we still gate this on a clean staging cycle.)
6. **Rollback strategy**: Steps 1–4 ship together. If something breaks in production-equivalent staging, revert step 3 (code change) — `Resolved*` columns can be ignored and the legacy columns still hold valid data because the backfill never modified them. Step 5 is only run after we're confident.

## Open Questions

- Should the `PriceResolver` expose a "what-if" mode that ignores `EffectiveFrom` to let operators preview a draft rule against future dates without saving? Likely yes; flagging for tasks but not blocking.
- Should `SlipPriceAdjustment` carry its own `EffectiveFrom`/`EffectiveTo`, or is scheduling reserved for rules only? Leaning **rules only** for v1 (keeps the model simpler), but worth confirming with operators in beta.
- For transient pricing, does the per-foot rate need a separate adjustment kind (`+$X per foot`) vs. flat (`+$X total`)? Current model uses flat-only on the adjustment row, which is enough but slightly less expressive than rules. Calling out so reviewers can push back if needed.
