## ADDED Requirements

### Requirement: Per-slip plan assignment

Each `Slip` SHALL carry a nullable `PricingPlanId` foreign key. When `PricingPlanId` is null the slip uses the marina's active default plan. When set it uses that specific plan. Changing a slip's plan assignment SHALL enqueue a resolved-price recompute job for that slip.

#### Scenario: Slip without explicit assignment uses marina default

- **GIVEN** slip S has `PricingPlanId = null`
- **AND** the marina's default plan has `TransientRateKind = Flat`, `TransientAmount = 120`
- **THEN** `S.ResolvedTransientBaseRate = 120`

#### Scenario: Slip with explicit plan overrides default

- **GIVEN** slip S has `PricingPlanId = P` (plan P: `TransientAmount = 200`)
- **AND** the marina's default plan has `TransientAmount = 120`
- **THEN** `S.ResolvedTransientBaseRate = 200` (plan P wins)

#### Scenario: Assigning a plan to a slip triggers recompute

- **WHEN** a manager sets `Slip S.PricingPlanId = P` via the slip update endpoint
- **THEN** a Hangfire job is enqueued to recompute `S.ResolvedTransientBaseRate` and `S.ResolvedLeaseBaseRate`
- **AND** the cached values are updated within the job's execution

### Requirement: Resolved price cache on Slip

The system SHALL maintain `Slip.ResolvedTransientBaseRate` (decimal?) and `Slip.ResolvedLeaseBaseRate` (decimal?) as cached computed columns. These values SHALL be recomputed by a Hangfire job in the following events:

- A plan's rate fields or add-ons are updated → recompute all slips assigned to that plan; if the plan is the default, also recompute all slips with `PricingPlanId IS NULL`
- A slip's `PricingPlanId` is changed → recompute that slip
- The marina's default plan is promoted → recompute all slips with `PricingPlanId IS NULL`

The resolver function SHALL be a pure function of (plan, slip amenity flags, slip dimensions) and SHALL be unit-testable without database access.

#### Scenario: Recompute triggered when plan rates change

- **WHEN** a manager updates plan P's `TransientAmount` from `120` to `135`
- **THEN** all slips assigned to plan P have their `ResolvedTransientBaseRate` updated to reflect the new rate
- **AND** if P is the default, slips with `PricingPlanId IS NULL` are also recomputed

#### Scenario: Null resolved rate excludes slip from search

- **WHEN** a plan has `TransientRateKind = null`
- **THEN** slips on that plan have `ResolvedTransientBaseRate = null`
- **AND** those slips do not appear in transient availability searches

### Requirement: Bulk plan assignment

The system SHALL provide `POST /marinas/{marinaId}/pricing/plans/bulk-assign` allowing a manager to assign a plan to many slips at once using a filter. The request body SHALL accept: `targetPlanId` (required), and optional filters: `dockId`, `minLength`, `maxLength`, `minBeam`, `maxBeam`, `amenities` (array of amenity enum values), `currentPlanId` (only reassign slips currently on this plan; use a sentinel value or null to match unassigned slips). The response SHALL return `{ assignedCount: int }`. A recompute job SHALL be enqueued for all affected slips.

#### Scenario: Bulk assign by dock and length range

- **WHEN** a manager posts `{ targetPlanId: P, dockId: D, minLength: 30, maxLength: 45 }`
- **THEN** all slips on dock D with `MaxLength` between 30 and 45 (inclusive) are assigned to plan P
- **AND** the response returns the count of slips updated
- **AND** a recompute job is enqueued for each updated slip

#### Scenario: Bulk assign only unassigned slips

- **WHEN** a manager posts `{ targetPlanId: P, currentPlanId: null }`
- **THEN** only slips with `PricingPlanId IS NULL` are assigned to plan P

#### Scenario: Bulk assign with amenity filter

- **WHEN** a manager posts `{ targetPlanId: P, amenities: ["Covered", "Electric50A"] }`
- **THEN** only slips where `IsCovered = true AND ElectricAmpsAvailable = 50` are assigned

#### Scenario: No matching slips returns zero count

- **WHEN** the filter matches no slips
- **THEN** the response is `200 OK` with `{ assignedCount: 0 }` and no recompute jobs are enqueued
