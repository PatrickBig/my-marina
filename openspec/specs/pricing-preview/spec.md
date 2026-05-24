## ADDED Requirements

### Requirement: Client-side price resolver
The frontend SHALL implement a pure TypeScript function `resolvePrice(plan, slipLength, slipBeam, amenities)` that mirrors the backend `PriceResolver.Resolve` logic exactly:
- `Flat` rate kind → resolved rate = `Amount`
- `PerFoot` rate kind → resolved rate = `Amount × slipLength`
- `PerArea` rate kind → resolved rate = `Amount × slipLength × slipBeam`
- Null `rateKind` → null resolved rate (rate not offered)
- For each active amenity add-on where the slip has that amenity, add `transientAmount` or `leaseAmount` to the respective resolved rate
- Apply `MinCharge` floor: `max(base + addOns, minCharge ?? 0)`
- Returns `{ transient: number | null, lease: number | null }`

#### Scenario: Flat rate resolves correctly
- **WHEN** rate kind is `Flat` with amount $120
- **THEN** resolved rate is $120 regardless of slip dimensions

#### Scenario: PerFoot rate resolves correctly
- **WHEN** rate kind is `PerFoot` with amount $3.50 and slip length 40ft
- **THEN** resolved rate is $140.00

#### Scenario: PerArea rate resolves correctly
- **WHEN** rate kind is `PerArea` with amount $0.18, slip length 40ft, and beam 14ft
- **THEN** resolved rate is $0.18 × 40 × 14 = $100.80

#### Scenario: Null rate kind returns null
- **WHEN** rate kind is null (not offered)
- **THEN** transient or lease resolved rate is null

#### Scenario: Amenity add-ons stack on base rate
- **WHEN** base transient rate is $100 and Electric50A add-on is $15 and slip has Electric50A
- **THEN** resolved transient rate is $115

#### Scenario: MinCharge floor applied
- **WHEN** computed rate is $40 but minCharge is $75
- **THEN** resolved rate is $75

#### Scenario: MinCharge not applied when base exceeds it
- **WHEN** computed rate is $200 and minCharge is $75
- **THEN** resolved rate is $200

---

### Requirement: Pricing preview panel in pricing forms
Both the setup wizard pricing step (`Step5Pricing`) and the pricing plan create/edit form (`PricingPlansPage`) SHALL include a `PricingPreviewPanel` component that displays computed rates in real time as the operator edits pricing fields.

The panel SHALL:
- Show two editable inputs for sample slip dimensions: **Length (ft)** and **Beam (ft)**, defaulting to 40 and 14 respectively
- Show toggle checkboxes for each amenity defined in the plan's add-ons (only show toggles for amenities that have a transient or lease add-on amount configured)
- Display the resolved **Transient rate** and **Lease rate** as formatted dollar amounts, labeled with their units (e.g., "$140.00 / night", "$3,500.00 / month")
- Show "—" when the rate kind is null (not offered)
- Recompute on every change to: rate kind, amount, min charge, amenity add-on amounts, sample dimensions, amenity toggles
- Be collapsed/hidden by default in the setup wizard (to keep the wizard lightweight), expanded by default on `PricingPlansPage`

#### Scenario: Preview updates on amount change
- **WHEN** operator changes the `TransientAmount` field
- **THEN** the preview panel immediately shows the new computed transient rate

#### Scenario: Preview reflects amenity add-ons
- **WHEN** operator sets an amenity add-on amount and toggles that amenity on in the preview
- **THEN** the preview shows base rate + add-on amount

#### Scenario: Preview shows "—" for un-offered rate
- **WHEN** `LeaseRateKind` is empty (not offered)
- **THEN** the lease rate row in the preview shows "—"

#### Scenario: Preview shows PerArea explanation
- **WHEN** rate kind is `PerArea`
- **THEN** the preview shows the breakdown formula (e.g., "$0.18 × 40ft × 14ft = $100.80")

#### Scenario: Operator changes sample dimensions
- **WHEN** operator changes the length input in the preview panel to 60
- **THEN** the displayed rate immediately recalculates using 60ft

#### Scenario: Wizard preview is collapsible
- **WHEN** wizard pricing step renders
- **THEN** preview panel is collapsed; operator can click "Preview rates" to expand it
