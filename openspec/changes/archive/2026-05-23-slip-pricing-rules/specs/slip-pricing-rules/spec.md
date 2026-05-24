## ADDED Requirements

### Requirement: Marina-scoped pricing rule entity

The system SHALL provide a `PricingRule` entity owned by a `Marina`. Each rule SHALL define a base or surcharge price that applies to slips matching its predicate, for a specific `ListingKind` (Transient or Lease), during an effective time window.

Each rule SHALL carry the following fields: `Id` (UUID v7), `MarinaId`, `Name` (operator-supplied string), `ContributionKind` (`Base` or `Surcharge`), `Priority` (signed integer; higher wins for `Base`), `Predicate` (flat value object stored as JSONB — listing kind, optional lease term, optional length/beam bounds, optional amenity requirements), `RateKind` (`Flat` or `PerFoot`), `Amount` (decimal), `MinCharge` (decimal nullable; only honored on `Base` rules), `EffectiveFrom` (timestamptz, defaults to `now()`), `EffectiveTo` (timestamptz nullable; null = open-ended), `CreatedAt`, `UpdatedAt`.

The system SHALL provide CRUD endpoints under `/marinas/{marinaId}/pricing/rules` accessible to users with a `Membership` at that marina with role `Owner` or `Manager`. List, get, create, update, and delete SHALL be supported.

#### Scenario: Marina manager creates a lease pricing rule

- **WHEN** a user with `Manager` membership at marina M creates a rule with `Name = "Annual lease, 30-40ft"`, `ContributionKind = Base`, `Priority = 100`, `Predicate = { ListingKind: Lease, LeaseTerm: Annual, MinLength: 30, MaxLength: 40 }`, `RateKind = Flat`, `Amount = 3200`, `EffectiveFrom = 2026-01-01`
- **THEN** the rule is persisted with a new UUID v7 `Id` and `MarinaId = M`
- **AND** the response includes the rule body

#### Scenario: Non-member cannot manage rules

- **WHEN** a user without a `Manager`/`Owner` membership at marina M calls `POST /marinas/{M}/pricing/rules`
- **THEN** the API returns `403 Forbidden`

#### Scenario: Demo tenant cannot write rules

- **WHEN** a request authenticated with `is_demo = true` calls any non-GET endpoint under `/marinas/{marinaId}/pricing/rules`
- **THEN** the `WriteAccess` policy returns `403 Forbidden`

### Requirement: Layered/stacking price resolution

The system SHALL provide a `PriceResolver` service that, given a `(slipId, listingKind, asOf)` triple, deterministically returns a resolved base rate.

Resolution SHALL proceed as follows:

1. Load every `PricingRule` for the slip's `MarinaId` whose `Predicate` matches the slip's attributes AND whose effective window contains `asOf` (`EffectiveFrom <= asOf` AND (`EffectiveTo IS NULL` OR `EffectiveTo > asOf`)).
2. Among matching rules with `ContributionKind = Base`, select exactly one: the rule with the highest `Priority`; ties broken by ascending `CreatedAt`. If no `Base` rule matches, the resolver SHALL return `null` (meaning "not listed for this listing kind").
3. Compute the base contribution from the selected `Base` rule: `Flat` → `Amount`; `PerFoot` → `Amount * Slip.MaxLength`.
4. Sum the contributions from every matching `Surcharge` rule, computed the same way.
5. Sum every `SlipPriceAdjustment` whose `SlipId = slipId` and `ListingKind = listingKind`.
6. If the selected `Base` rule has a non-null `MinCharge`, the final result SHALL be `Max(sum, MinCharge)`.

The resolver SHALL be a pure function of its inputs (rule rows, adjustment rows, slip attributes) — calling it twice with the same data and the same `asOf` MUST return the same number.

#### Scenario: Base rule plus two surcharges plus one adjustment

- **GIVEN** a slip with `MaxLength = 35` at marina M
- **AND** a `Base` rule "Annual lease 30-40ft" with `RateKind = Flat`, `Amount = 3000`, `Priority = 100`
- **AND** a `Surcharge` rule "Has electric" with `RateKind = Flat`, `Amount = 200`, matching slips with `HasElectric = true`
- **AND** a `Surcharge` rule "Covered" with `RateKind = Flat`, `Amount = 150`, matching slips with `IsCovered = true`
- **AND** a `SlipPriceAdjustment` "Deepwater" with `Amount = 100` on the slip for `ListingKind = Lease`
- **AND** the slip has `HasElectric = true` and `IsCovered = true`
- **WHEN** `PriceResolver.Resolve(slipId, Lease, asOf: 2026-06-01)` is called
- **THEN** the result is `3000 + 200 + 150 + 100 = 3450`

#### Scenario: Highest-priority base rule wins

- **GIVEN** a slip that matches two `Base` rules: "Cheap default" with `Priority = 0`, `Amount = 2500`, and "Premium 30-40ft" with `Priority = 100`, `Amount = 3000`
- **WHEN** the resolver runs
- **THEN** the base contribution is `3000` (only "Premium 30-40ft" is selected)
- **AND** "Cheap default" contributes nothing

#### Scenario: No matching base rule produces a null price

- **WHEN** the resolver runs against a slip for which no `Base` rule's predicate matches
- **THEN** the result is `null`
- **AND** the slip is treated as "not listed" for that listing kind

#### Scenario: MinCharge floor applies after stacking

- **GIVEN** a `Base` rule with `RateKind = PerFoot`, `Amount = 5`, `MinCharge = 250`
- **AND** a slip with `MaxLength = 20` (per-foot total = 100, no surcharges, no adjustments)
- **WHEN** the resolver runs
- **THEN** the result is `250` (the floor)

#### Scenario: PerFoot rule multiplies by slip MaxLength

- **GIVEN** a `Base` rule with `RateKind = PerFoot`, `Amount = 100`, `MinCharge = null`
- **AND** a slip with `MaxLength = 35`
- **WHEN** the resolver runs
- **THEN** the base contribution is `100 * 35 = 3500`

### Requirement: Effective-window scheduling of rules

The system SHALL allow `PricingRule` records to be created with `EffectiveFrom` set to any future date and `EffectiveTo` set to any future date after `EffectiveFrom`. Rules whose effective window does not contain the current time SHALL NOT contribute to resolved prices.

A `PricingRule` with `EffectiveFrom > now()` SHALL only be writable by tenants whose `SubscriptionTier >= Pro`. Free-tier marinas attempting to schedule a future-dated rule SHALL receive `403 Forbidden` with a tier-upgrade message.

#### Scenario: Pro-tier marina schedules a price increase for next year

- **GIVEN** marina M is `Pro` tier; today is 2026-05-22
- **WHEN** a manager creates a rule with `EffectiveFrom = 2027-01-01` and `Amount = 3300`, while an existing rule with `EffectiveFrom = 2026-01-01`, `EffectiveTo = 2027-01-01`, `Amount = 3200` is in place
- **THEN** the new rule is persisted
- **AND** resolving the slip on 2026-12-31 returns `3200`
- **AND** resolving the slip on 2027-01-02 returns `3300`

#### Scenario: Free-tier marina cannot schedule future-dated rules

- **GIVEN** marina M is `Free` tier
- **WHEN** a manager submits `POST /marinas/{M}/pricing/rules` with `EffectiveFrom = 2027-01-01`
- **THEN** the API returns `403 Forbidden` with body `{ "code": "tier_required", "requiredTier": "Pro" }`

#### Scenario: Free-tier marina can create rules effective today

- **GIVEN** marina M is `Free` tier
- **WHEN** a manager submits a rule with `EffectiveFrom` omitted (defaults to `now()`)
- **THEN** the rule is persisted normally

### Requirement: Rule preview against a sample slip

The system SHALL provide `GET /marinas/{marinaId}/pricing/rules/preview?slipId={slipId}&listingKind={kind}&asOf={iso}` that returns, for the given slip and listing kind at the given timestamp, every contributing rule (id, name, contribution kind, amount), every contributing adjustment (id, label, amount), and the final resolved price. This endpoint SHALL be readable by anyone with a `Manager`/`Owner` membership at the marina.

#### Scenario: Preview shows the full price breakdown

- **WHEN** a manager calls `GET /marinas/{M}/pricing/rules/preview?slipId={S}&listingKind=Lease&asOf=2027-01-15`
- **THEN** the response includes a `base` rule object, an array `surcharges` of zero or more rules, an array `adjustments` of zero or more adjustments, a `minChargeApplied` boolean, and a `total` decimal
- **AND** the response uses the rule set effective on 2027-01-15, not today

#### Scenario: Demo tenant slip cannot leak into preview for a real marina

- **WHEN** a request authenticated against marina M calls preview with `slipId` belonging to a demo-tenant marina
- **THEN** the API returns `404 Not Found`
