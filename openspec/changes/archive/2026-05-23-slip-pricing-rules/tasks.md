## 1. Domain model

- [x] 1.1 Add `MyMarina.Domain/Entities/PricingRule.cs` with all fields per `slip-pricing-rules` spec, including `ContributionKind`, `Priority`, `Predicate`, `EffectiveFrom`/`EffectiveTo`
- [x] 1.2 Add `MyMarina.Domain/Enums/ContributionKind.cs` (`Base = 0`, `Surcharge = 1`)
- [x] 1.3 Add `MyMarina.Domain/ValueObjects/PricingRulePredicate.cs` as a flat record (listing kind, lease term, length/beam bounds, amenity flags)
- [x] 1.4 Add `MyMarina.Domain/Entities/SlipPriceAdjustment.cs` per `slip-price-adjustments` spec
- [x] 1.5 Modify `MyMarina.Domain/Entities/Slip.cs`: add `ResolvedTransientBaseRate`, `ResolvedLeaseBaseRate`, `ResolvedAsOf`; remove the `DefaultTransient*` and `DefaultLease*` properties

## 2. EF persistence

- [x] 2.1 Add `MyMarina.Infrastructure/Persistence/Configurations/PricingRuleConfiguration.cs`: table, JSONB column for `Predicate`, indexes on `(MarinaId, EffectiveFrom, EffectiveTo)` and `(MarinaId, ContributionKind, Priority DESC)`
- [x] 2.2 Add `MyMarina.Infrastructure/Persistence/Configurations/SlipPriceAdjustmentConfiguration.cs` with index on `(SlipId, ListingKind)`
- [x] 2.3 Update `SlipConfiguration.cs`: drop the legacy default-price column mappings (after the safe drop migration in 2.6); map the three new `Resolved*` columns
- [x] 2.4 Register `DbSet<PricingRule>` and `DbSet<SlipPriceAdjustment>` in `AppDbContext` with the standard tenant global query filter (rules filter via `Marina.TenantId`)
- [x] 2.5 Generate schema migration `Phase19_PricingRules` via `dotnet ef migrations add` — creates the two new tables and adds the three new `Resolved*` columns to `Slip`; keeps legacy columns in place
- [x] 2.6 Generate data migration `Phase19_PricingRulesBackfill` — superseded: `Phase19_PricingRules` already dropped/renamed all legacy columns; no live data to backfill (app not yet in production)
- [x] 2.7 Generate cleanup migration `Phase19_PricingRulesDropLegacy` — superseded: legacy columns already removed in `Phase19_PricingRules`

## 3. Application layer

- [x] 3.1 Create `MyMarina.Application/Marinas/PricingRules/` module: `PricingRuleDtos.cs`, `PricingRuleCommands.cs` (Create/Update/Delete), `PricingRuleQueries.cs` (List/Get/Preview)
- [x] 3.2 Create `MyMarina.Application/Slips/PriceAdjustments/` module: DTOs, Commands (Create/Update/Delete), Queries (List/Get)
- [x] 3.3 Add `MyMarina.Application/Pricing/IPriceResolver.cs` abstraction with `Resolve(Guid slipId, ListingKind kind, DateTimeOffset asOf)` returning `decimal?` plus a richer `ResolveWithBreakdown(...)` for the preview endpoint
- [x] 3.4 Add FluentValidation rules: `EffectiveTo > EffectiveFrom`, `Priority` not int.MaxValue/MinValue (reserve for engine use), at least one predicate clause set, `LeaseTerm` required when `Predicate.ListingKind = Lease`, `Amount >= 0` on `Base` rules

## 4. Resolver implementation

- [x] 4.1 Implement `MyMarina.Infrastructure/Pricing/PriceResolver.cs` following the layered algorithm in `slip-pricing-rules` spec — pure function, takes rule rows + adjustment rows + slip + asOf
- [x] 4.2 Add unit tests in `tests/MyMarina.UnitTests/Pricing/PriceResolverTests.cs` covering every scenario in the `slip-pricing-rules` spec plus edge cases: no matching base, ties on priority broken by CreatedAt, MinCharge floor, PerFoot math, negative adjustments
- [x] 4.3 Register `IPriceResolver` → `PriceResolver` via Scrutor

## 5. Background jobs

- [x] 5.1 Add `MyMarina.Infrastructure/Pricing/PricingResolverJob.cs` — accepts a set of slip IDs (or a marina + predicate) and recomputes `Resolved*` columns by calling `IPriceResolver`
- [x] 5.2 Add `MyMarina.Infrastructure/Pricing/PricingResolverSweepJob.cs` — reads its last-run timestamp from a `PricingResolverSweepState` table (single row), finds rules with `EffectiveFrom` or `EffectiveTo` in `(lastRun, now]`, enqueues `PricingResolverJob` for matching slips, advances timestamp
- [x] 5.3 Register both jobs in `MyMarina.Infrastructure/Messaging/HangfireConfig.cs`; configure sweep as a recurring job with hourly cron
- [x] 5.4 Wire targeted enqueue into the rule/adjustment command handlers via `IMessageBus.Enqueue(...)`; on rule edit, enqueue with predicate **union of old + new**
- [x] 5.5 Integration test `PricingResolverSweepJobTests`: rule with `EffectiveFrom` in the past hour activates on next sweep; verify idempotency

## 6. API surface

- [x] 6.1 Add `MyMarina.Api/Controllers/PricingRulesController.cs` with routes under `/marinas/{marinaId}/pricing/rules` (list, get, create, update, delete, preview) — all non-GET routes wear `[WriteAccess]` decorator
- [x] 6.2 Add `MyMarina.Api/Controllers/SlipPriceAdjustmentsController.cs` under `/slips/{slipId}/price-adjustments`
- [x] 6.3 Enforce manager/owner membership via existing authorization helpers in `IUserContext.HasMarinaAccess(...)`
- [x] 6.4 Apply `[RequiresTier(SubscriptionTier.Pro)]` conditionally for future-dated rule writes (custom action filter that inspects `EffectiveFrom`)
- [x] 6.5 Update `MyMarina.Api/Controllers/MarinasController.cs` slip-update endpoint to reject writes to legacy price fields with `400 Bad Request` and a migration message in the body (this is the breaking change boundary)

## 7. Read-path migration

- [x] 7.1 Update `MyMarina.Infrastructure/Search/SlipAvailabilityFilter.cs`: price predicate switches from `DefaultTransientBaseRate`/`DefaultLeaseBaseRate` to `ResolvedTransientBaseRate`/`ResolvedLeaseBaseRate`; null-resolved-rate slips excluded
- [x] 7.2 Update `MyMarina.Infrastructure/Search/MarinaRollupSearchQueryHandler.cs` price predicate identically
- [x] 7.3 Update `MyMarina.Infrastructure/Search/GetPublicSlipDetailQueryHandler.cs` to surface `Resolved*` values to the slip detail DTO; drop the old `DefaultTransient*` fields from the DTO
- [x] 7.4 Update `MyMarina.Infrastructure/Reservations/CreateReservationCommandHandler.cs` and `MyMarina.Infrastructure/Leases/CreateLeaseInquiryCommandHandler.cs` to read `Resolved*` instead of `Default*`
- [x] 7.5 Update `MyMarina.Infrastructure/Marinas/UpdateSlipCommandHandler.cs` to no longer accept price-field writes (matches API rejection in 6.5)

## 8. Renewal flow

- [x] 8.1 Audit `SlipAssignment` creation/renewal handlers; introduce a `RenewSlipAssignmentCommand` (or extend the existing one) that resolves price via `IPriceResolver` for `asOf = newStartDate`
- [x] 8.2 Integration test `SlipAssignmentRenewalTests`: in-flight assignment retains its `BaseRate`; renewal at a date crossing a scheduled rule picks up the new price; resolver returning null produces `409 Conflict`

## 9. Tier gating and demo

- [x] 9.1 Add `Capabilities.ScheduledPricing` constant to `MyMarina.Infrastructure/Demo/TierCapabilityRegistry.cs`; assign to `Pro` and `Premium`
- [x] 9.2 Update `MyMarina.Infrastructure/Demo/DemoSeedScript.cs` to seed ~5 representative `PricingRule` rows (mix of `Base`/`Surcharge`, range of size brackets, one scheduled future-dated rule) plus ~10 `SlipPriceAdjustment` rows on demo slips
- [x] 9.3 Update the demo-seed integration test (the one asserting at least one record per entity) to cover `PricingRule` and `SlipPriceAdjustment`
- [x] 9.4 Verify `WriteAccess` decorator on the new controllers — add an integration test that a demo JWT receives `403` on any non-GET pricing endpoint

## 10. Frontend (MyMarina.Web)

- [x] 10.1 Run `dotnet watch --project src/MyMarina.Api` and `npm run generate-api` in `src/MyMarina.Web/` to regenerate `src/api/schema.d.ts`
- [x] 10.2 Add new TanStack Router route `/marinas/$marinaId/pricing` and page `src/pages/marinas/PricingRulesPage.tsx` — rule list, create/edit dialog, schedule controls, predicate editor (size brackets, amenity toggles, listing kind, lease term)
- [x] 10.3 Add `src/components/pricing/PreviewPriceCard.tsx` consuming the preview endpoint — picks a sample slip, shows base + surcharges + adjustments breakdown
- [x] 10.4 Add `src/pages/slips/SlipAdjustmentsTab.tsx` accessible from slip detail — list/create/edit/delete adjustments
- [x] 10.5 Remove the per-slip price inputs from `src/pages/slips/SlipEditForm.tsx`; replace with a read-only resolved-price display linking to the marina pricing rules page
- [x] 10.6 Gate the schedule UI (`EffectiveFrom` date picker) behind the `subscription_tier` claim — Free tier sees the field disabled with an upgrade hint
- [x] 10.7 Add `src/api/queries/pricing.ts` with TanStack Query hooks for rules and adjustments; ensure mutations invalidate slip detail/search queries
- [x] 10.8 Use the `playwright-cli` skill to capture screenshots of the new pricing admin UI and the slip adjustments tab; commit them under `src/MyMarina.Marketing/public/screenshots/` and reference from `ScreenshotsSection` (per CLAUDE.md screenshot rule)

## 11. End-to-end verification

- [x] 11.1 Integration test `PricingRuleLifecycleTests`: create rule → resolver job runs → matching slips have non-null `Resolved*` → search returns slips → price filter respects resolved value → edit rule → recompute reflects in search
- [x] 11.2 Integration test `ScheduledPricingActivationTests`: schedule a future-dated rule → manually trigger sweep with mocked clock → matching slips' resolved prices update
- [x] 11.3 Integration test `LegacyBackfillTests` — superseded: no backfill migration exists (legacy columns dropped directly in Phase19_PricingRules; app not in production)
- [x] 11.4 Run full `dotnet test` — 45 unit tests + 19 pricing/demo integration tests pass; 9 pre-existing photo integration test failures are unrelated to pricing
- [x] 11.5 Use the `verify` skill to manually walk the marina pricing UI end to end in a browser: create a rule, schedule a future rule, add an adjustment, confirm slip detail shows the resolved price

