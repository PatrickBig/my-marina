## ADDED Requirements

### Requirement: Core boolean amenity fields on Slip
The `Slip` entity SHALL gain three new boolean fields: `HasPumpOut` (default `false`), `IsCovered` (default `false`), and `IsIndoor` (default `false`). These SHALL be included in all slip read responses, slip create/update payloads, and the bulk setup endpoint. Existing slip records created before this migration SHALL default to `false` for all three fields. The `DemoSeedScript` SHALL be updated in the same migration PR to set realistic values on seeded slips.

#### Scenario: Slip created with pump-out
- **WHEN** a slip is created with `HasPumpOut = true`
- **THEN** the slip detail response SHALL include `"hasPumpOut": true`

#### Scenario: Existing slips default to false
- **WHEN** the migration runs against existing slip records
- **THEN** `HasPumpOut`, `IsCovered`, and `IsIndoor` SHALL all be `false` for pre-existing slips

#### Scenario: Demo seed reflects realistic amenities
- **WHEN** `DemoSeedScript.SeedAsync` runs
- **THEN** at least one seeded slip SHALL have `HasPumpOut = true` and at least one SHALL have `IsCovered = true`

### Requirement: Custom amenity tags on Slip
The `Slip` entity SHALL gain an `Amenities` field of type `string[]` stored as jsonb. This field holds marina-defined freeform tags (e.g., "Fuel dock", "Wi-Fi", "Guest lounge access"). The field SHALL default to an empty array. Tags are unstructured text and are not filterable in marketplace search in MVP. All slip read responses SHALL include the `amenities` array. Create and update payloads SHALL accept an `amenities` array.

#### Scenario: Slip created with custom tags
- **WHEN** a slip is created with `amenities: ["Fuel dock", "Wi-Fi"]`
- **THEN** the slip detail response SHALL include `"amenities": ["Fuel dock", "Wi-Fi"]`

#### Scenario: Slip with empty amenities
- **WHEN** a slip is created without specifying `amenities`
- **THEN** the slip detail response SHALL include `"amenities": []`

#### Scenario: Tags are not filterable in search
- **WHEN** a boater performs a slip search
- **THEN** the search endpoint SHALL NOT accept an `amenities` filter parameter in MVP
- **THEN** custom tags SHALL be visible on the slip detail page only

### Requirement: Amenity defaults in the onboarding wizard
The dock builder in the onboarding wizard SHALL include fields for all Slip amenity properties as dock-level defaults: `HasElectric`, `Electric` (enum), `HasWater`, `HasPumpOut`, `IsCovered`, `IsIndoor`, and an "Add custom tag" input for the `Amenities` array. Core amenities SHALL be presented as labeled checkboxes. Custom tags SHALL support adding and removing freeform strings. These defaults SHALL propagate to all slips in the dock when the structure is generated, and SHALL be overridable per-slip in the preview table.

#### Scenario: Core amenity defaults set for a dock
- **WHEN** the user checks "Has pump-out" and "Covered" as defaults for Dock A
- **THEN** all generated slips in Dock A SHALL have `HasPumpOut = true` and `IsCovered = true`

#### Scenario: Custom tag added at dock level
- **WHEN** the user adds "Fuel dock" as a custom tag default for Dock B
- **THEN** all generated slips in Dock B SHALL have `amenities` containing "Fuel dock"

#### Scenario: Per-slip amenity override in preview
- **WHEN** the user unchecks `IsCovered` on a single slip in the preview table
- **THEN** only that slip SHALL have `IsCovered = false` while other slips in the dock retain the default
