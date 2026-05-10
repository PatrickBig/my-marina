## Why

New marina owners currently hit a flat two-field form that creates a Tenant and Marina with no docks, no slips, and no guidance — then land on a dashboard with nothing in it. Setting up a real marina (15 docks × 20 slips = 300 individual records) requires hundreds of manual clicks through the dashboard, making activation painful enough that operators are likely to abandon before their marina is useful. A guided onboarding wizard with bulk dock/slip generation removes the primary friction between signup and a working marina.

## What Changes

- **New: Marina draft state** — `Marina.IsSetupComplete` and `Marina.SetupStep` fields; draft marinas are invisible to all external views (marketplace, search) until the owner explicitly publishes.
- **New: Multi-step marina setup wizard** — replaces the flat `MarinaOnboardingPage` form; guides owners through profile, GPS location, dock/slip bulk builder, preview/adjust, and publish steps.
- **New: Geocoder with Leaflet map** — Nominatim address-to-coordinates lookup with progressive fallback chain; draggable pin for GPS fine-tuning.
- **New: Dock/slip bulk builder** — extensible naming-convention system for docks and slips; dock-level dimension/amenity defaults; localStorage crash buffer with step-level backend sync.
- **New: Preview & adjust table** — live-editable dock/slip grid grouped by dock with bulk-edit and inline-edit; backed by a new `PUT /marinas/{id}/setup/docks` batch endpoint.
- **New: Publish step** — explicit opt-in to marketplace listing with education copy; defaults to not listed.
- **New: Home page draft card** — dismissible setup banner for users with zero marinas; draft marina card variant with "Continue setup" and "Delete draft" actions.
- **Extended: Slip amenities** — adds `HasPumpOut`, `IsCovered`, `IsIndoor` (bool) and `Amenities` (string[]) to the `Slip` entity; dock-level defaults in the wizard.

## Capabilities

### New Capabilities

- `marina-onboarding-wizard`: Multi-step wizard flow — draft marina lifecycle, profile step, GPS/geocoder step, publish step, home page entry points, localStorage persistence.
- `dock-slip-bulk-setup`: Dock/slip bulk builder — naming convention system, per-dock defaults, preview/adjust table, `PUT /marinas/{id}/setup/docks` batch endpoint.
- `slip-amenities`: Extended amenity model on Slip — core boolean fields (pump-out, covered, indoor) plus a freeform string-array tags column for marina-defined amenities.

### Modified Capabilities

_(none — no existing spec-level requirements are changing)_

## Impact

- **Domain / Infrastructure**: `Marina` entity gains `IsSetupComplete` (bool) and `SetupStep` (int); `Slip` entity gains `HasPumpOut`, `IsCovered`, `IsIndoor` (bool) and `Amenities` (string[], jsonb). EF Core migration required.
- **API**: New `PUT /marinas/{id}/setup/docks` endpoint (bulk replace draft dock+slip tree); `DELETE /marinas/{id}` unlocked for draft marinas. OpenAPI spec regeneration required after backend changes.
- **Global query filters**: Draft marina filter added — `IsSetupComplete = false` marinas excluded from all non-owner queries.
- **Frontend**: New wizard page at `/marina/{id}/setup`; refactored `HomePage` with draft card and banner; Leaflet draggable-pin component extracted from `SearchPage` for reuse; Nominatim geocoder utility; naming convention generator utilities; localStorage persistence layer.
- **DemoSeedScript**: Must be updated in the same PR to reflect new `Slip` amenity columns and `Marina.IsSetupComplete = true` on seeded marinas.
