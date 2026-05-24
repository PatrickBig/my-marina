## 1. Teardown Old Pricing System

- [x] 1.1 Remove `PricingRule` entity, EF configuration, and all command/query handlers
- [x] 1.2 Remove `SlipPriceAdjustment` entity, EF configuration, and all command/query handlers
- [x] 1.3 Remove `PricingResolverSweepJob`, `PricingResolverJob`, and `PricingResolverSweepState` Hangfire jobs
- [x] 1.4 Remove `PricingRulesController` and `SlipPriceAdjustmentsController`
- [x] 1.5 Remove `Slip.ResolvedTransientRateKind`, `Slip.ResolvedLeaseRateKind`, and `Slip.ResolvedAsOf` from the `Slip` entity (keep `ResolvedTransientBaseRate`/`ResolvedLeaseBaseRate` — they are re-added in task 3)
- [x] 1.6 Remove old frontend pages and components: `PricingRulesPage.tsx`, `SlipAdjustmentsTab.tsx`, `PreviewPriceCard.tsx`, and `src/api/queries/pricing.ts` (if it exists as a standalone file)
- [x] 1.7 Remove all route registrations and nav links pointing to old pricing pages
- [x] 1.8 Verify the project still compiles after teardown (fix any remaining references)

## 2. Domain — PricingPlan Entity and Supporting Types

- [x] 2.1 Add `PerArea = 2` to the `RateKind` enum alongside existing `Flat = 0` and `PerFoot = 1`
- [x] 2.2 Add `PlanAmenity` enum: `Covered`, `Electric30A`, `Electric50A`, `HasWater`, `HasPumpOut`
- [x] 2.3 Create `AmenityAddOn` value object with fields: `Amenity` (PlanAmenity), `TransientAmount` (decimal?), `LeaseAmount` (decimal?)
- [x] 2.4 Create `PricingPlan` entity: `Id` (UUID v7), `MarinaId`, `TenantId`, `Name` (max 200), `IsDefault` (bool), `TransientRateKind` (RateKind?), `TransientAmount` (decimal?), `TransientMinCharge` (decimal?), `LeaseRateKind` (RateKind?), `LeaseAmount` (decimal?), `LeaseMinCharge` (decimal?), `AmenityAddOns` (ICollection\<AmenityAddOn\>), `CreatedAt`, `UpdatedAt`
- [x] 2.5 Add `Slip.PricingPlanId` (Guid?, nullable FK) and `Slip.PricingPlan` navigation property
- [x] 2.6 Re-add `Slip.ResolvedTransientBaseRate` (decimal?) and `Slip.ResolvedLeaseBaseRate` (decimal?) for the event-driven price cache

## 3. EF Core Configuration

- [x] 3.1 Create `PricingPlanConfiguration`: table `pricing_plans`, unique index on `(MarinaId, Name)`, HasQueryFilter matching `r => r.Marina.IsSetupComplete` pattern, `OwnsMany(p => p.AmenityAddOns).ToJson("add_ons")` for JSONB storage
- [x] 3.2 Add `IsDefault` unique partial index: only one plan per marina may have `IsDefault = true` (enforce at DB level via a filtered unique index or via application logic; at minimum enforce via application)
- [x] 3.3 Update `SlipConfiguration`: add `PricingPlanId` FK column, navigation to `PricingPlan`, and the two resolved-rate decimal? columns
- [x] 3.4 Register `PricingPlan` in `AppDbContext.OnModelCreating` and expose `DbSet<PricingPlan>`

## 4. Migration Reset

- [x] 4.1 Delete all files under `src/MyMarina.Infrastructure/Persistence/Migrations/` and `AppDbContextModelSnapshot.cs`
- [x] 4.2 Drop the dev Postgres database: `docker exec my-marina-postgres-1 psql -U mymarina -c "DROP DATABASE mymarina;"`
- [x] 4.3 Generate the single new migration: `dotnet ef migrations add InitialSchema --project src/MyMarina.Infrastructure --startup-project src/MyMarina.Api`
- [x] 4.4 Apply the migration: `dotnet ef database update --project src/MyMarina.Infrastructure --startup-project src/MyMarina.Api`
- [ ] 4.5 Re-run demo seed to verify schema is clean: `dotnet run --project src/MyMarina.Api -- seed` (or via startup seed path)

## 5. Price Resolver

- [x] 5.1 Create `PriceResolver` static class in Application layer with method `Resolve(PricingPlan plan, decimal? slipLength, decimal? slipBeam, IEnumerable<PlanAmenity> slipAmenities) → (decimal? transient, decimal? lease)`
- [x] 5.2 Implement base rate calculation: `Flat` = Amount, `PerFoot` = Amount × MaxLength, `PerArea` = Amount × MaxLength × MaxBeam; null RateKind → null resolved rate
- [x] 5.3 Implement add-on summation: for each `AmenityAddOn` where slip has that amenity, add `TransientAmount` / `LeaseAmount` to the respective resolved rate
- [x] 5.4 Implement MinCharge floor: `max(base + add_ons, MinCharge ?? 0)`
- [x] 5.5 Write unit tests covering: Flat rate, PerFoot rate, PerArea rate, add-on stacking, MinCharge floor applied, MinCharge not applied when base exceeds it, null RateKind → null result

## 6. Recompute Hangfire Jobs

- [x] 6.1 Create `RecomputeSlipPriceJob(Guid slipId)`: loads slip + its effective plan (own plan or marina default), calls `PriceResolver`, updates `ResolvedTransientBaseRate` and `ResolvedLeaseBaseRate`
- [x] 6.2 Create `RecomputePlanSlipPricesJob(Guid planId)`: loads all slips assigned to that plan, enqueues `RecomputeSlipPriceJob` for each
- [x] 6.3 Create `RecomputeDefaultPlanSlipPricesJob(Guid marinaId)`: loads all slips in the marina with `PricingPlanId IS NULL`, enqueues `RecomputeSlipPriceJob` for each
- [x] 6.4 Register all three jobs with Hangfire

## 7. PricingPlans API — CRUD

- [x] 7.1 Create `PricingPlansController` with `[Authorize]` and marina membership guard (Owner or Manager)
- [x] 7.2 `GET /marinas/{marinaId}/pricing/plans` — list all plans for the marina with their add-ons
- [x] 7.3 `GET /marinas/{marinaId}/pricing/plans/{planId}` — get single plan
- [x] 7.4 `POST /marinas/{marinaId}/pricing/plans` — create plan; if `IsDefault = true` and another default exists, swap atomically in one transaction; if `IsDefault = true` and no default exists, set directly
- [x] 7.5 `PUT /marinas/{marinaId}/pricing/plans/{planId}` — update plan rates/add-ons; enqueue `RecomputePlanSlipPricesJob` and (if plan is default) `RecomputeDefaultPlanSlipPricesJob`
- [x] 7.6 `DELETE /marinas/{marinaId}/pricing/plans/{planId}` — reject with `409 Conflict` if plan is default; for non-default plans, set all assigned slips' `PricingPlanId = null`, enqueue recompute for affected slips, then delete
- [x] 7.7 `POST /marinas/{marinaId}/pricing/plans/{planId}/set-default` — atomically demote current default, promote this plan; enqueue `RecomputeDefaultPlanSlipPricesJob` for newly-null slips

## 8. PricingPlans API — Bulk Assign

- [x] 8.1 `POST /marinas/{marinaId}/pricing/plans/bulk-assign` — accepts `{ targetPlanId, dockId?, minLength?, maxLength?, minBeam?, maxBeam?, amenities?, currentPlanId? }` body
- [x] 8.2 Build dynamic EF query applying each non-null filter (dock, length/beam range, amenity flags, currentPlanId sentinel for unassigned slips)
- [x] 8.3 Bulk-update matching slips' `PricingPlanId` to `targetPlanId`; enqueue `RecomputeSlipPriceJob` for each updated slip
- [x] 8.4 Return `{ assignedCount: int }` (200 OK even when zero matches)

## 9. Slip Update — Plan Assignment

- [x] 9.1 Extend the existing slip update endpoint/handler to accept optional `PricingPlanId` (Guid?) in the request body
- [x] 9.2 When `PricingPlanId` changes, validate the new plan belongs to the same marina, then enqueue `RecomputeSlipPriceJob` for that slip

## 10. Marina Compliance — Listing Guard

- [x] 10.1 In the Marina update handler where `IsListed` is set to `true`, query for an `IsDefault = true` plan; return `422 Unprocessable Entity` if none exists
- [x] 10.2 Add compliance check to `MarinasController` or handler with a clear error message

## 11. Demo Seed Update

- [x] 11.1 Remove all `PricingRule` and `SlipPriceAdjustment` seed entries from `DemoSeedScript`
- [x] 11.2 Seed a default pricing plan for the demo marina (e.g., `Standard` plan: `Flat`, transient $120/night, lease $2800/month, `IsDefault = true`)
- [x] 11.3 Seed one or two additional plans with different rate kinds (e.g., `Premium` with PerFoot lease, `PerArea` transient plan) including amenity add-ons (covered, electric)
- [x] 11.4 Assign a subset of demo slips explicitly to the non-default plans so bulk-assign and per-slip assignment are visible in the demo

## 12. Frontend — PricingPlansPage

- [x] 12.1 Create `PricingPlansPage.tsx` at the existing `/marina/{marinaId}/pricing` route (replacing the old rules page)
- [x] 12.2 Implement plan list: show name, rate summary (transient + lease), IsDefault badge, and action buttons (Edit, Set Default, Delete)
- [x] 12.3 Implement create/edit plan form: name, TransientRateKind + Amount + MinCharge, LeaseRateKind + Amount + MinCharge, amenity add-on table (per amenity: transient amount, lease amount)
- [x] 12.4 Wire Set Default action to `POST .../set-default` and refresh list
- [x] 12.5 Wire Delete action (disabled for default plan) to `DELETE .../{planId}` with confirmation dialog
- [x] 12.6 Add "Bulk assign" button per plan that opens the bulk-assign dialog

## 13. Frontend — Bulk Assign Dialog

- [x] 13.1 Create `BulkAssignDialog.tsx` with filter inputs: dock selector, min/max length, min/max beam, amenity checkboxes, current plan selector (with "unassigned only" option)
- [x] 13.2 Submit filter + targetPlanId to `POST .../bulk-assign` and show `"X slips updated"` toast on success

## 14. Frontend — Setup Wizard Pricing Step

- [x] 14.1 Add a new "Default pricing plan" step (Step 5) to `MarinaSetupWizardPage.tsx`, inserting it between the existing slip preview step (Step 4) and the photos step — renaming photos to Step 6 and publish to Step 7; update `STEPS` array and all `setStep` calls accordingly
- [x] 14.2 Implement `Step5Pricing` component: condensed plan creation form with name, transient rate kind + amount + optional min charge, lease rate kind + amount + optional min charge, and a simplified amenity add-on table (one row per amenity: transient $, lease $)
- [x] 14.3 On submit, call `POST /marinas/{marinaId}/pricing/plans` with `IsDefault: true`; on success advance to Step 6 (photos)
- [x] 14.4 Add "Skip for now" link that advances to Step 6 without creating a plan; persist skipped state so the publish step knows no plan exists
- [x] 14.5 In the publish step (Step 7), fetch pricing plans for the marina; if no plan with `IsDefault = true` exists, show an inline notice and disable the "List on marketplace" toggle with explanatory copy

## 15. Frontend — Marina Dashboard Compliance Warning

- [x] 15.1 On the marina dashboard, fetch pricing plans list and show a prominent warning banner if no plan has `IsDefault = true`
- [x] 15.2 Warning banner includes a link to the pricing plans page (`/marina/{id}/pricing`)
- [x] 15.3 Update the "Pricing rules →" dashboard link to point to the new `/marina/{id}/pricing` route

## 16. API Types and Build

- [ ] 16.1 Run `npm run generate-api` from `src/MyMarina.Web/` after all backend contract changes are in place to regenerate `schema.d.ts`
- [x] 16.2 Verify `dotnet build` and `npm run build` pass with no errors or warnings

## 17. Integration Tests

- [x] 17.1 Test plan CRUD: create plan, update rates, verify recompute enqueued, delete non-default plan (slips fall back to null)
- [x] 17.2 Test default plan enforcement: promote plan demotes previous default in single transaction
- [x] 17.3 Test listing compliance: attempt `IsListed = true` without a default plan returns `422`
- [x] 17.4 Test bulk assign: filter by dock + length range → correct slips updated, correct count returned
- [x] 17.5 Test price resolver: unit tests for all RateKind values, add-on stacking, and MinCharge floor (see task 5.5)
- [x] 17.6 Test slip assignment renewal uses resolved lease rate; no resolved rate returns 409
