## Context

The current pricing system (`PricingRule` + predicate engine) was implemented but never used in production. It is being replaced with a simpler plan-based model before any live data exists, making a clean migration feasible. The new model must support: named pricing plans with both transient and lease rates, per-amenity flat surcharges, a default plan fallback, explicit per-slip plan assignment, bulk assignment by filter, and cached resolved prices for search performance.

The app is pre-production, so all EF Core migrations can be collapsed to a single `InitialSchema` migration and the dev database dropped and recreated.

## Goals / Non-Goals

**Goals:**
- Replace predicate-based rules with explicit plan assignment that operators can reason about intuitively
- Support `Flat`, `PerFoot` (×MaxLength), and `PerArea` (×MaxLength×MaxBeam) rate kinds
- Support per-amenity add-ons (always flat, separate amounts per listing kind) within each plan
- Exactly one default plan per marina at any time; explicit promotion replaces any succession mechanism
- Bulk-assign slips to a plan via dock/size/amenity/current-plan filters
- Cache resolved transient and lease prices on the `Slip` row for search query performance; recompute event-driven
- Marina compliance check: no default plan → prominent dashboard warning + marketplace blocked
- Reset migrations to a single `InitialSchema`

**Non-Goals:**
- Scheduled automatic plan rotation (Pro-tier "schedule default swap" is a future change)
- Per-slip price adjustments on top of plan rates (removed; variation handled by creating a dedicated plan)
- Preserving any data from the old `pricing_rules` / `slip_price_adjustments` tables (pre-production, no live data)
- Changing the search API contract for callers (price filter still uses cached slip columns)

## Decisions

### 1. PricingPlan as a named, stable bundle — no effective dates on plans

**Decision:** Plans have no `EffectiveFrom`/`EffectiveTo`. They are permanent named bundles. Rate changes are handled by editing the plan or assigning slips to a different plan.

**Rationale:** Effective dates on non-default plans introduce ambiguity about what happens to assigned slips when a plan "expires." Keeping plans stable eliminates that edge case entirely. The default plan promotion mechanism handles the only temporal concern (switching the marina-wide fallback).

**Alternative considered:** Time-windowed plans (as in the old `PricingRule` design) — rejected because it pushes scheduling complexity onto every plan rather than just the default handoff.

### 2. Default promotion — explicit swap, no background job

**Decision:** `PricingPlan.IsDefault` is a boolean; a marina has exactly one `IsDefault = true` plan at any time. Promoting a plan sets it to `true` and sets the previous default to `false` in a single transaction. No background job; no effective-date-based auto-rotation.

**Rationale:** Background jobs introduce timing jitter, race conditions, and invisible state transitions. Explicit promotion is auditable, testable, and maps directly to the operator's intent ("I'm switching the default now"). Scheduled promotion (future Pro-tier feature) can be layered on top without redesigning the core model.

**Compliance enforcement:** If a marina has no `IsDefault = true` plan, the system treats it as non-compliant: `IsListed` is forced to `false` on the next listing attempt, and a prominent warning appears on the marina dashboard. Operators are directed to promote a plan.

### 3. Amenity add-ons — flat amounts only, separate per listing kind

**Decision:** `AmenityAddOn` records within a plan carry `TransientAmount: decimal?` and `LeaseAmount: decimal?`; no `RateKind` field. Both amounts are nullable — an add-on may apply to only one listing kind.

**Rationale:** Variable-rate add-ons ("$0.50/sq-ft for covered") are hard for operators to reason about and explain to customers. Flat amounts are transparent. The plan's base rate already handles size-based complexity via `PerFoot`/`PerArea`.

**Amenity enum values:** `Covered`, `Electric30A`, `Electric50A`, `HasWater`, `HasPumpOut`. Stored as a string discriminator in JSONB. Adding new amenities later is a minor schema change to the enum only.

### 4. Add-ons stored as JSONB on PricingPlan

**Decision:** `AmenityAddOn` collection is stored as a JSONB column (`add_ons`) on the `pricing_plans` table via EF Core `OwnsMany(...).ToJson(...)`.

**Rationale:** The collection is always loaded with its parent plan, has no independent query needs, and rarely exceeds a handful of entries. A separate join table would add complexity with no benefit.

### 5. RateKind.PerArea — W × L formula

**Decision:** `PerArea` rate = `Amount × Slip.MaxLength × Slip.MaxBeam`. The enum value `PerArea = 2` is added alongside `Flat = 0` and `PerFoot = 1`.

**Rationale:** Some marina operators charge by gross footprint (e.g., wide T-heads). `MaxLength × MaxBeam` is the standard billing footprint used by marina management software.

### 6. Cached resolved prices — event-driven recomputation

**Decision:** `Slip.ResolvedTransientBaseRate` and `Slip.ResolvedLeaseBaseRate` (decimal? columns) are retained for search performance. They are recomputed by enqueuing a Hangfire job from command handlers whenever:
- A plan's rate fields or add-ons change → recompute all slips assigned to that plan (and all unassigned slips if it is the default)
- A slip's `PricingPlanId` changes → recompute that slip
- The marina's default plan changes (promotion) → recompute all slips with `PricingPlanId IS NULL`

No sweep job. Resolution is a pure function: plan rates + slip amenity flags + slip dimensions → decimal?.

**Resolved price formula:**
```
base = plan.Amount (Flat) | plan.Amount × slip.MaxLength (PerFoot) | plan.Amount × slip.MaxLength × slip.MaxBeam (PerArea)
add_ons = sum of add-on amounts where slip has that amenity
resolved = max(base + add_ons, plan.MinCharge ?? 0)
```

### 7. Migration reset

**Decision:** Delete all existing migration files and `AppDbContextModelSnapshot.cs`. Generate a single new `InitialSchema` migration. Drop and recreate the dev Postgres database.

**Rationale:** The app is pre-production with no live data. Carrying 19 incremental migrations is pure debt. A single clean migration is easier to review, reason about, and maintain.

**Procedure:**
1. Drop the dev database: `docker exec my-marina-postgres-1 psql -U mymarina -c "DROP DATABASE mymarina;"`
2. Delete `src/MyMarina.Infrastructure/Persistence/Migrations/` contents
3. Run `dotnet ef migrations add InitialSchema` to regenerate from current model
4. Run `dotnet ef database update` to apply

### 8. Bulk assign endpoint

**Decision:** `POST /marinas/{marinaId}/pricing/plans/bulk-assign` accepts a filter body (`dockId?`, `minLength?`, `maxLength?`, `minBeam?`, `maxBeam?`, `amenities?`, `currentPlanId?`) and a `targetPlanId`. Returns count of slips updated. Enqueues resolved-price recomputation for all affected slips.

**Rationale:** A dedicated endpoint is cleaner than a generic slip-update with plan override. The filter mirrors the bulk-assign UI's filter panel, making the API a direct projection of user intent.

## Risks / Trade-offs

- **No per-slip exception pricing:** Removing `SlipPriceAdjustment` means any price difference requires a new plan. For marinas with many one-off exceptions, this could mean many plans. Mitigation: bulk-assign makes plan proliferation manageable; amenity add-ons handle the most common differentiation.
- **JSONB add-ons not individually queryable:** Filtering slips by "has a plan with a covered add-on > $X" requires a JSONB path query. Not needed for MVP. Mitigation: if needed later, add a generated column or separate table.
- **Event-driven recompute coverage:** A missed enqueue leaves a stale cached price. Mitigation: keep the `ResolvedAsOf` timestamp on the slip; a lightweight sanity-check admin endpoint can surface stale records.
- **Migration reset loses test fixtures:** Any integration test data seeded against the old schema is invalidated. Mitigation: tests use `Testcontainers` with per-run schema apply — already clean.

## Migration Plan

1. Delete old pricing entities, handlers, controllers, frontend components (see proposal Impact section)
2. Add `PricingPlan` entity and configuration; add `Slip.PricingPlanId`; add `RateKind.PerArea`
3. Drop and recreate dev database; generate `InitialSchema` migration
4. Implement `PriceResolver` (plan-based), command handlers, `PricingPlansController`
5. Update demo seed: remove old rule seed, add representative pricing plans
6. Build frontend: `PricingPlansPage`, bulk-assign UI, update marina dashboard compliance warning
7. Regenerate `schema.d.ts`

## Open Questions

- None — all design decisions were resolved during the exploration session.
