## Context

The current marina onboarding flow (`MarinaOnboardingPage.tsx` + `POST /auth/signup/marina`) creates a Tenant and Marina in a single flat form with three fields (org name, marina name, type). No docks or slips are created. After submission the user lands on an empty dashboard with no guidance. Setting up a real marina — typically 5–20 docks with 10–30 slips each — requires hundreds of individual dashboard interactions.

The frontend uses simple path-based routing in `App.tsx` (no TanStack Router). The backend uses controller-based APIs with `ICommandHandler`/`IQueryHandler`. The app already uses Leaflet + OpenStreetMap for the slip search map. There are no existing specs for marina onboarding.

This change is cross-cutting: new EF entities/columns, new API endpoints, new frontend pages and shared utilities, and a modification to the global query filter behavior for draft marinas.

## Goals / Non-Goals

**Goals:**
- Add `IsSetupComplete` and `SetupStep` to `Marina`; filter drafts from all external queries
- Add `HasPumpOut`, `IsCovered`, `IsIndoor`, and `Amenities` to `Slip`
- New wizard at `/marina/{id}/setup` covering profile, GPS, dock/slip builder, preview/adjust, publish
- Extensible naming convention generator system (dock + slip)
- localStorage crash buffer with step-level backend sync
- `PUT /marinas/{id}/setup/docks` batch endpoint (transactional, idempotent replace)
- Draggable Leaflet pin component extracted from `SearchPage` for reuse
- Nominatim geocoder utility with progressive address fallback
- Home page draft card + dismissible setup banner

**Non-Goals:**
- Boater orientation wizard
- Spreadsheet import UI (endpoint contract is designed for it, UI is future)
- Full EAV amenity attribute system (string[] tags are MVP)
- Dockominium / private dock wizard changes
- Marketplace amenity filtering

## Decisions

### D1: Draft marina modeled via `IsSetupComplete` bool, not a status enum

**Decision:** Add `Marina.IsSetupComplete` (bool, default `false`) and `Marina.SetupStep` (int, default `0`).

**Rationale:** A bool cleanly separates "setup incomplete" from `IsListed` (owner chose not to list). An enum (`Draft`, `Active`, `Suspended`) would add states we don't need yet and invite premature complexity. `SetupStep` as an int is sufficient to track resume position without encoding business logic in the DB.

**Alternative considered:** Reuse `IsListed = false` as the proxy for draft — rejected because it conflates "not yet set up" with "intentionally unlisted," making it impossible to distinguish after publish.

**EF filter:** Add `&& (EF.Property<bool>(m, "IsSetupComplete") || isOwner)` to the Marina global query filter, where `isOwner` derives from `IUserContext.Memberships`. Platform operators bypass the filter as usual.

---

### D2: Wizard state in localStorage with step-level backend sync

**Decision:** localStorage (keyed `marina-setup-{marinaId}`) is the working state during the wizard. Backend is synced on step transitions and explicit "Save progress" button clicks, not on every keystroke.

**Rationale:** Continuous sync on every change would require debounce + conflict resolution and could cause race conditions during fast edits in the preview table. localStorage writes are synchronous and instant. The backend is the durable record; localStorage is the crash buffer.

**Load priority:** On wizard load, compare `localStorage.timestamp` vs `Marina.UpdatedAt` from the API. Use whichever is newer. If localStorage is absent, load from backend.

**Alternative considered:** Backend-only persistence with no localStorage — rejected because a connection drop mid-preview table edit (after 2 hours of slip entry) would be extremely frustrating.

---

### D3: Batch endpoint replaces entire draft tree atomically

**Decision:** `PUT /marinas/{id}/setup/docks` accepts the full dock+slip tree and replaces all existing draft docks/slips in a single database transaction. Existing individual PATCH/DELETE slip endpoints handle post-preview inline edits.

**Rationale:** A replace-all pattern is simpler to reason about than a diff-and-patch approach. During setup the dock structure hasn't been shared with anyone, so overwriting it is safe. The transaction guarantee means no partial state can exist.

**Payload shape** (designed to be forward-compatible with spreadsheet import):
```json
{
  "docks": [
    {
      "name": "Dock A",
      "slips": [
        {
          "name": "A-1",
          "maxLength": 35.0,
          "maxBeam": 12.0,
          "maxDraft": 6.0,
          "slipType": "Floating",
          "hasElectric": true,
          "electric": "Amp30",
          "hasWater": true,
          "hasPumpOut": false,
          "isCovered": false,
          "isIndoor": false,
          "amenities": []
        }
      ]
    }
  ]
}
```

**Guard:** Returns 409 if `Marina.IsSetupComplete = true` — batch replace is only valid on drafts.

**Alternative considered:** Incremental per-dock POST using existing CRUD endpoints — rejected because it requires the client to track which docks already exist and produces orphan records on partial failure.

---

### D4: Naming conventions as pure generator functions

**Decision:** Each naming convention is a pure function with the signature:
- `generateDockName(convention: DockConvention, index: number, config: ConventionConfig): string`
- `generateSlipName(convention: SlipConvention, dockName: string, dockIndex: number, slipIndex: number, totalSlipsBefore: number, config: ConventionConfig): string`

Conventions are a discriminated union type. Adding a new convention = one new generator + one UI `case` — no changes to callers.

**Supported dock conventions:** `Lettered` (A, B, C… + optional prefix/suffix), `Numbered` (1, 2, 3… + optional prefix/suffix), `Manual`.

**Supported slip conventions:** `PerDockReset` (A-1…A-10, B-1…B-10), `PerDockGlobal` (A-1…A-10, B-11…B-20), `Sequential` (1…300), `Manual`.

**Config params:** `prefix`, `suffix`, `separator`, `startAt`, `padZeros`.

**Alternative considered:** A class hierarchy with override methods — rejected as unnecessary for pure name generation; functions are simpler to test and compose.

---

### D5: Nominatim geocoder with progressive address fallback

**Decision:** Use Nominatim (OpenStreetMap) for geocoding. Trigger is an explicit "Locate on map" button (not auto-geocode on blur). Fallback chain: (1) full address, (2) city+state+zip, (3) city+state, (4) state only. On match, display precision level to the user. On no match, show informational message and leave the map for manual pin placement.

**Rationale:** Nominatim is free, requires no API key, and is consistent with the Leaflet + OSM stack already in production. The explicit button avoids geocoding mid-type. The fallback chain ensures the user always gets a map starting point rather than a blank screen.

**Leaflet pin:** Extract the map component from `SearchPage.tsx` into a reusable `MapPicker` component that accepts `lat`, `lng`, `onPositionChange` props and renders a single draggable marker. `SearchPage` can be refactored to use it.

**Nominatim rate limiting:** Add a `User-Agent` header identifying the app (required by Nominatim's usage policy). The geocoder is user-triggered so rate limiting is not a concern in practice.

---

### D6: Slip amenities — expand core columns + string[] tags

**Decision:** Add `HasPumpOut` (bool), `IsCovered` (bool), `IsIndoor` (bool), and `Amenities` (string[], jsonb) to the `Slip` entity via EF migration.

**Rationale:** Option A (explicit columns + unstructured tags) is the right MVP trade-off. The three new boolean fields cover the most common marina amenities that affect a boater's decision. The `Amenities` string array handles everything else without schema complexity. A full EAV attribute system is a future upgrade path when marketplace amenity filtering becomes a requirement.

**Migration:** All existing slips default to `false` / `[]`. DemoSeedScript is updated in the same PR with realistic amenity values.

**Alternative considered:** Full attribute system (`AmenityDefinition` + `SlipAmenityValue` tables) — rejected as overkill for MVP; the query complexity outweighs the benefit when marketplace amenity filtering isn't yet a requirement.

---

### D7: Home page draft detection

**Decision:** The existing `GET /me/marinas` (or equivalent) endpoint SHALL include `isSetupComplete` and `setupStep` in the marina DTO. The home page renders draft marinas in a distinct card variant and shows the dismissible banner when the user has zero non-draft marinas.

**Banner dismiss:** Stored in `localStorage` as `marina-setup-banner-dismissed`. Not persisted to the backend — if the user clears storage the banner reappears, which is acceptable.

## Risks / Trade-offs

- **localStorage loss** → User loses unsaved preview table edits since the last step-sync. Mitigation: "Save progress" button is prominently visible in the wizard header. Step transitions always sync.
- **Nominatim precision** → Address data may be incomplete for some marina locations. Mitigation: progressive fallback + manual pin mode ensures the user always has a path to set coordinates.
- **Large slip counts in preview table** → 300+ rows may cause render performance issues. Mitigation: virtual scrolling (e.g., TanStack Virtual) can be added if profiling shows it's needed; dock collapse reduces visible rows significantly.
- **PUT /setup/docks on large payloads** → 300 slips × fields is ~30KB JSON — well within HTTP limits. The transactional delete+insert for 300 rows is fast enough that no chunking is needed.
- **Draft marina accumulation** → Users who abandon setup leave draft records. Mitigation: the "Delete draft" action on the home page card makes cleanup easy; a future platform-operator cleanup job can purge stale drafts (> 90 days, no activity).

## Migration Plan

1. Add EF Core migration for `Marina.IsSetupComplete`, `Marina.SetupStep`, and the four new `Slip` columns.
2. Update `DemoSeedScript.SeedAsync` — set `IsSetupComplete = true` on all seeded marinas; populate new slip amenity fields with realistic demo data.
3. Update EF global query filter for `Marina` to exclude drafts from non-owner queries.
4. Deploy backend (new columns are additive; existing marinas default to `IsSetupComplete = false`, which will hide them from external search — **this is a concern**).

> ⚠️ **Deployment order matters:** The Marina filter defaults all existing marinas to `IsSetupComplete = false` on migration. A **data migration** must run immediately after schema migration to set `IsSetupComplete = true` on all pre-existing marinas (via `migrationBuilder.Sql("UPDATE \"Marinas\" SET \"IsSetupComplete\" = true WHERE \"IsSetupComplete\" = false")`). This is the one acceptable `migrationBuilder.Sql` call per CLAUDE.md policy.

5. Deploy frontend wizard pages and updated home page.
6. Regenerate `schema.d.ts` from running API after backend deployment.

**Rollback:** Revert the migration (drops new columns) and revert frontend deployment. No data loss since new columns are additive with defaults.

## Open Questions

- None — all design decisions were resolved during the exploration session.
