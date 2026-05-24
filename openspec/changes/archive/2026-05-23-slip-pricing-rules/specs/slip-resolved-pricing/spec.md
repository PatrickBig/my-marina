## ADDED Requirements

### Requirement: Resolved price materialized on each slip

The system SHALL maintain three computed columns on each `Slip`: `ResolvedTransientBaseRate` (decimal nullable), `ResolvedLeaseBaseRate` (decimal nullable), and `ResolvedAsOf` (timestamptz, the timestamp the resolver evaluated against — usually `now()` at recompute time).

These columns SHALL be the **only** source of truth for slip price reads in the application. Every read path — slip detail, slip search, marina rollup search, reservation pricing, lease inquiry creation — SHALL consume these columns directly and SHALL NOT invoke the `PriceResolver` inline per row.

A null value in either resolved column SHALL be interpreted as "this slip is not listed for that listing kind." Search queries SHALL exclude slips with a null resolved rate for the listing kind being searched.

#### Scenario: Slip search reads resolved rates without resolver call

- **WHEN** `GET /marinas/{M}/slips/search?listingKind=Lease&priceMin=3000&priceMax=3500` is executed
- **THEN** the underlying SQL filters on `Slip.ResolvedLeaseBaseRate BETWEEN 3000 AND 3500`
- **AND** the resolver service is not invoked during the query

#### Scenario: Slip with null resolved rate is excluded from search

- **GIVEN** slip S has `ResolvedLeaseBaseRate = NULL`
- **WHEN** a boater searches for lease slips
- **THEN** slip S is excluded from the result set

### Requirement: Targeted recompute on rule and adjustment changes

The system SHALL enqueue a Hangfire job named `PricingResolverJob` whenever a `PricingRule` or `SlipPriceAdjustment` is created, updated, or deleted. The job SHALL recompute the resolved prices of:

- All slips at the rule's marina whose attributes match the rule's predicate, when a rule changes (the predicate is evaluated against the **union** of the rule's prior and new predicate if the change was an edit, so slips that no longer match still get refreshed).
- The single slip referenced by the adjustment, when an adjustment changes.

The job SHALL be idempotent — re-running it on the same data set SHALL produce the same resolved values.

#### Scenario: Editing a rule predicate refreshes both old and new matching slips

- **GIVEN** a rule R currently matches slips with `MaxLength` between 30 and 40
- **WHEN** a manager edits R to match `MaxLength` between 35 and 45
- **THEN** the `PricingResolverJob` is enqueued for all slips with `MaxLength` between 30 and 45 inclusive
- **AND** slips in 30-34 (no longer matching R) get a fresh resolve that excludes R
- **AND** slips in 41-45 (newly matching R) get a fresh resolve that includes R

#### Scenario: Targeted recompute completes within seconds for a 300-slip marina

- **WHEN** a manager edits a rule at a marina with 300 slips matching its predicate
- **THEN** `PricingResolverJob` completes the recompute and writes new `Resolved*` values within 10 seconds under nominal load

### Requirement: Hourly sweep activates scheduled rules

The system SHALL register a recurring Hangfire job `PricingResolverSweepJob` that runs every hour. On each invocation, the sweep SHALL identify rules whose `EffectiveFrom` or `EffectiveTo` boundary crossed since the prior sweep timestamp, and SHALL enqueue `PricingResolverJob` for slips matched by the affected rules' predicates.

The sweep SHALL persist its last-run timestamp so that the boundary-crossed query is bounded.

#### Scenario: A scheduled rule activates automatically

- **GIVEN** a rule R has `EffectiveFrom = 2027-01-01T00:00:00Z` and `EffectiveTo = NULL`, increasing the lease rate from 3200 to 3300
- **AND** the prior sweep ran at 2026-12-31T23:00:00Z
- **WHEN** the sweep runs at 2027-01-01T00:00:00Z
- **THEN** all slips matching R's predicate are enqueued for recompute
- **AND** their `ResolvedLeaseBaseRate` becomes `3300` (assuming no other contributors changed)

#### Scenario: Sweep is a no-op when no boundaries crossed

- **GIVEN** no rules have an `EffectiveFrom` or `EffectiveTo` in the (prior-sweep, now] window
- **WHEN** the sweep runs
- **THEN** no `PricingResolverJob` is enqueued
- **AND** the last-run timestamp is advanced to now

### Requirement: Renewal-time pricing reads the rule engine

When a `SlipAssignment` is renewed (either through a manual renewal command or any future automatic renewal flow), the system SHALL call `PriceResolver.Resolve(slipId, ListingKind, asOf: renewalDate)` and SHALL set the new assignment's `BaseRate` to that value. The system SHALL NOT modify the `BaseRate` of any existing in-flight `SlipAssignment` based on rule changes.

If the resolver returns `null` at the renewal date (the slip is no longer listed under matching rules), the renewal SHALL fail with a `409 Conflict` and a message instructing the operator to create a rule or adjustment covering the slip before the renewal date.

#### Scenario: Year-end lease renewal picks up the scheduled price

- **GIVEN** an active `SlipAssignment` with `BaseRate = 3200`, `EndDate = 2026-12-31`
- **AND** a `PricingRule` effective 2027-01-01 producing `3300` for that slip
- **WHEN** the manager renews the assignment with a new term starting 2027-01-01
- **THEN** the new `SlipAssignment` has `BaseRate = 3300`
- **AND** the original assignment retains its `BaseRate = 3200`

#### Scenario: In-flight assignment is not touched by rule changes

- **GIVEN** an active `SlipAssignment` with `BaseRate = 3200`, `EndDate = 2026-12-31`
- **WHEN** a manager edits the rule that originally produced `3200` so it now produces `3300`
- **THEN** the active assignment's `BaseRate` remains `3200`
- **AND** only the slip's `Resolved*` columns are recomputed
