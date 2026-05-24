## ADDED Requirements

### Requirement: Slip-level additive price adjustments

The system SHALL provide a `SlipPriceAdjustment` entity owned by a `Slip`. Each adjustment SHALL define a named additive delta that contributes to that slip's resolved price for a specific `ListingKind`.

Each adjustment SHALL carry: `Id` (UUID v7), `SlipId`, `ListingKind` (`Transient` or `Lease`), `Label` (operator-supplied string, e.g., "Deepwater" or "Endcap view"), `Amount` (decimal; negative values permitted to model discounts), `CreatedAt`, `UpdatedAt`.

The system SHALL provide CRUD endpoints under `/slips/{slipId}/price-adjustments`. Access SHALL require a `Manager`/`Owner` membership at the slip's `MarinaId`.

A slip MAY have any number of adjustments. Adjustments SHALL NOT have an effective window in v1 — they apply unconditionally for as long as they exist.

#### Scenario: Manager adds a deepwater adjustment to a specific slip

- **WHEN** a manager at marina M creates an adjustment for slip S with `Label = "Deepwater"`, `Amount = 200`, `ListingKind = Lease`
- **THEN** the adjustment is persisted with a new `Id`
- **AND** subsequent resolves of slip S for `ListingKind = Lease` include `+200` in the total

#### Scenario: Adjustment for a different listing kind does not apply

- **GIVEN** a `Lease` adjustment of `+200` exists on slip S
- **WHEN** the resolver runs for `ListingKind = Transient`
- **THEN** the adjustment is NOT included in the total

#### Scenario: Negative adjustment models a discount

- **GIVEN** a `Lease` base rule producing `3200` for slip S
- **AND** a `SlipPriceAdjustment` on S with `Amount = -200`, `ListingKind = Lease`, `Label = "Awkward access discount"`
- **WHEN** the resolver runs
- **THEN** the resolved price is `3000`

#### Scenario: Non-manager cannot create adjustments

- **WHEN** a user without `Manager`/`Owner` membership at the slip's marina calls `POST /slips/{slipId}/price-adjustments`
- **THEN** the API returns `403 Forbidden`

#### Scenario: Demo tenant cannot write adjustments

- **WHEN** a request with `is_demo = true` calls any non-GET endpoint under `/slips/{slipId}/price-adjustments`
- **THEN** the `WriteAccess` policy returns `403 Forbidden`

### Requirement: Editing or deleting an adjustment triggers price recompute

The system SHALL enqueue a `PricingResolverJob` for the affected slip whenever a `SlipPriceAdjustment` is created, updated, or deleted. The job SHALL update the slip's `ResolvedTransientBaseRate` / `ResolvedLeaseBaseRate` / `ResolvedAsOf` columns.

#### Scenario: Deleting an adjustment lowers the resolved price

- **GIVEN** slip S has `ResolvedLeaseBaseRate = 3400` (rule base 3200 + adjustment 200)
- **WHEN** the manager deletes the `+200` adjustment
- **THEN** within one recompute cycle, `ResolvedLeaseBaseRate` becomes `3200`
