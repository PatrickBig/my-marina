## ADDED Requirements

### Requirement: PricingPlan entity

The system SHALL provide a `PricingPlan` entity owned by a `Marina`. Each plan SHALL define a named bundle of rates and amenity add-ons covering both transient and lease listing kinds. Plans are permanent and have no effective-date window.

Each plan SHALL carry: `Id` (UUID v7), `MarinaId`, `TenantId`, `Name` (max 200 chars), `IsDefault` (bool), `TransientRateKind` (`Flat | PerFoot | PerArea`, nullable — null means plan does not support transient bookings), `TransientAmount` (decimal?, required when `TransientRateKind` is set), `TransientMinCharge` (decimal?, nullable), `LeaseRateKind` (nullable), `LeaseAmount` (decimal?, required when `LeaseRateKind` is set), `LeaseMinCharge` (decimal?, nullable), `AmenityAddOns` (collection stored as JSONB), `CreatedAt`, `UpdatedAt`.

Rate kind semantics:
- `Flat`: resolved amount = `Amount`
- `PerFoot`: resolved amount = `Amount × Slip.MaxLength`
- `PerArea`: resolved amount = `Amount × Slip.MaxLength × Slip.MaxBeam`

#### Scenario: Manager creates a plan with transient and lease rates

- **WHEN** a user with `Manager` membership at marina M creates a plan with `Name = "Standard"`, `IsDefault = true`, `TransientRateKind = Flat`, `TransientAmount = 120`, `LeaseRateKind = PerFoot`, `LeaseAmount = 85`, `LeaseMinCharge = 2500`
- **THEN** the plan is persisted with a new UUID v7 `Id` and `MarinaId = M`
- **AND** the response includes the full plan body

#### Scenario: Plan with PerArea rate kind

- **WHEN** a manager creates a plan with `TransientRateKind = PerArea`, `TransientAmount = 4.50`
- **AND** it is assigned to a slip with `MaxLength = 40`, `MaxBeam = 16`
- **THEN** the resolved transient price is `4.50 × 40 × 16 = 2880`

#### Scenario: Plan supporting only lease (transient rates null)

- **WHEN** a plan has `TransientRateKind = null`
- **AND** it is assigned to a slip
- **THEN** `Slip.ResolvedTransientBaseRate` is `null` and the slip does not appear in transient search results

### Requirement: Amenity add-ons on a plan

Each `PricingPlan` SHALL carry an `AmenityAddOns` collection stored as JSONB. Each add-on SHALL specify: `Amenity` (enum: `Covered | Electric30A | Electric50A | HasWater | HasPumpOut`), `TransientAmount` (decimal?, flat surcharge per night for transient), `LeaseAmount` (decimal?, flat surcharge per period for lease). Both amounts are nullable; an add-on may apply to one or both listing kinds.

When resolving a price for a slip, all add-ons whose `Amenity` matches an amenity the slip actually has SHALL be summed and added to the base rate. Add-on amounts are always flat regardless of the plan's `RateKind`.

#### Scenario: Covered and electric add-ons stack

- **GIVEN** a plan with `LeaseAmount = 2800` (Flat), add-ons: `{Covered, LeaseAmount: 50}`, `{Electric50A, LeaseAmount: 30}`
- **AND** a slip with `IsCovered = true`, `ElectricAmpsAvailable = 50`
- **THEN** the resolved lease price is `2800 + 50 + 30 = 2880`

#### Scenario: Add-on does not apply when slip lacks amenity

- **GIVEN** the same plan
- **AND** a slip with `IsCovered = false`, `ElectricAmpsAvailable = 0`
- **THEN** the resolved lease price is `2800`

#### Scenario: Add-on with only a transient amount does not affect lease

- **GIVEN** an add-on `{HasWater, TransientAmount: 3, LeaseAmount: null}`
- **AND** a slip with `HasWater = true`
- **THEN** the transient resolved price includes `+3`
- **AND** the lease resolved price is not affected by this add-on

### Requirement: MinCharge floor

When a plan has a non-null `TransientMinCharge` or `LeaseMinCharge`, the resolved price for that listing kind SHALL be `Max(base + add_ons, MinCharge)`.

#### Scenario: PerFoot plan with MinCharge floor

- **GIVEN** a plan with `LeaseRateKind = PerFoot`, `LeaseAmount = 80`, `LeaseMinCharge = 2800`
- **AND** a slip with `MaxLength = 30` (per-foot total = 2400, no add-ons)
- **THEN** the resolved lease price is `2800` (floor applied)

#### Scenario: MinCharge not applied when base already exceeds it

- **GIVEN** the same plan
- **AND** a slip with `MaxLength = 40` (per-foot total = 3200)
- **THEN** the resolved lease price is `3200` (floor not applied)

### Requirement: Default plan and compliance

Each marina SHALL designate exactly one `PricingPlan` as its default via `IsDefault = true`. The default plan is the fallback for any slip with no explicit plan assignment.

A marina with no `IsDefault = true` plan is non-compliant. Non-compliant marinas SHALL NOT be listable in the marketplace: any attempt to set `Marina.IsListed = true` SHALL be rejected with `422 Unprocessable Entity`. A prominent compliance warning SHALL be shown on the marina operator dashboard whenever no default plan exists.

#### Scenario: Promoting a plan to default demotes the previous default

- **WHEN** a manager calls `POST /marinas/{M}/pricing/plans/{planId}/set-default`
- **AND** plan X is currently `IsDefault = true`
- **THEN** plan X becomes `IsDefault = false` and the promoted plan becomes `IsDefault = true` in a single transaction

#### Scenario: Marina cannot list without a default plan

- **WHEN** a marina has no `IsDefault = true` plan
- **AND** a manager attempts to set `IsListed = true`
- **THEN** the API returns `422 Unprocessable Entity` with a message indicating a default pricing plan is required

#### Scenario: First plan created can be set as default immediately

- **WHEN** a marina has no plans and a manager creates the first plan with `IsDefault = true`
- **THEN** the plan is created and designated as default with no conflict

### Requirement: Default pricing plan step in marina setup wizard

The marina setup wizard SHALL include an optional "Default pricing plan" step inserted between the slip preview step and the photos step. The step SHALL allow the operator to create the marina's first pricing plan and designate it as the default. The step SHALL be skippable; if skipped, no plan is created and the marina remains non-compliant until a default plan is configured later from the dashboard.

The step SHALL present a condensed plan creation form: plan name, transient rate (kind + amount + optional min charge), lease rate (kind + amount + optional min charge), and a simplified amenity add-on table. On submit, the system SHALL call `POST /marinas/{marinaId}/pricing/plans` with `IsDefault = true`. The resulting plan is the marina's default for all slips.

The publish step (final wizard step) SHALL check whether a default plan exists. If none exists, the publish step SHALL display an inline notice explaining that the marina cannot be listed on the marketplace until a default pricing plan is configured, and the "List on marketplace" toggle SHALL be disabled (but the operator may still finish setup without listing).

#### Scenario: Operator creates default plan during setup

- **WHEN** an operator fills in the pricing step during wizard setup and submits
- **THEN** a new `PricingPlan` is created with `IsDefault = true` and `MarinaId` set to the wizard's marina
- **AND** the wizard advances to the photos step

#### Scenario: Operator skips pricing step

- **WHEN** an operator clicks "Skip for now" on the pricing step
- **THEN** no plan is created
- **AND** the wizard advances to the photos step
- **AND** the publish step shows a notice: "A default pricing plan is required to list on the marketplace"
- **AND** the "List on marketplace" toggle is disabled

#### Scenario: Listing toggle disabled without default plan

- **GIVEN** no default plan exists when the operator reaches the publish step
- **THEN** the "List on marketplace" checkbox is disabled with explanatory copy
- **AND** the operator can still click "Finish setup" to complete onboarding without listing

### Requirement: PricingPlan CRUD endpoints

The system SHALL provide CRUD endpoints under `/marinas/{marinaId}/pricing/plans` accessible to users with `Owner` or `Manager` membership at that marina. List, get, create, update, delete, and `set-default` (POST) SHALL be supported.

Deleting the active default plan SHALL be rejected with `409 Conflict`. A plan with slips assigned to it MAY be deleted; those slips SHALL be implicitly reassigned to the default plan (their `PricingPlanId` set to null).

#### Scenario: Non-member cannot manage plans

- **WHEN** a user without a `Manager`/`Owner` membership at marina M calls any write endpoint under `/marinas/{M}/pricing/plans`
- **THEN** the API returns `403 Forbidden`

#### Scenario: Demo tenant cannot write plans

- **WHEN** a request with `is_demo = true` calls any non-GET endpoint
- **THEN** the `DemoWriteBlockFilter` returns `403 Forbidden`

#### Scenario: Deleting default plan is rejected

- **WHEN** a manager calls `DELETE /marinas/{M}/pricing/plans/{planId}` where `planId` is `IsDefault = true`
- **THEN** the API returns `409 Conflict`

#### Scenario: Deleting a plan with assigned slips reassigns those slips to default

- **WHEN** a manager deletes plan P which has 12 slips assigned
- **AND** the marina has a default plan D
- **THEN** all 12 slips have their `PricingPlanId` set to null (falling back to default D)
- **AND** their resolved prices are recomputed against plan D
