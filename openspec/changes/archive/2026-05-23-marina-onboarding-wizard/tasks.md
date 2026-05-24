## 1. Data Model & Migration

- [x] 1.1 Add `IsSetupComplete` (bool, default `false`) and `SetupStep` (int, default `0`) to `Marina` entity and EF configuration
- [x] 1.2 Add `HasPumpOut` (bool, default `false`), `IsCovered` (bool, default `false`), `IsIndoor` (bool, default `false`), and `Amenities` (string[], jsonb, default `[]`) to `Slip` entity and EF configuration
- [x] 1.3 Run `dotnet ef migrations add Phase17_MarinaOnboardingWizard` to generate the migration
- [x] 1.4 Add `migrationBuilder.Sql("UPDATE \"Marinas\" SET \"IsSetupComplete\" = true WHERE \"IsSetupComplete\" = false")` to the migration to backfill existing marinas
- [x] 1.5 Update `DemoSeedScript.SeedAsync` — set `IsSetupComplete = true` on all seeded marinas and populate new slip amenity fields with realistic demo values
- [x] 1.6 Run `dotnet ef database update` and verify CI integration test still passes (all entity types present in demo tenant)

## 2. Backend — Draft Marina Filter & Delete

- [x] 2.1 Update the EF Core global query filter on `Marina` to exclude `IsSetupComplete = false` marinas from non-owner queries (owner check via `IUserContext.Memberships`)
- [x] 2.2 Update `GET /me/marinas` (or equivalent) DTO to include `isSetupComplete` and `setupStep` fields
- [x] 2.3 Add `DELETE /marinas/{id}` endpoint that allows deletion when `IsSetupComplete = false`; cascade-deletes docks, slips, and memberships; returns 409 if marina is active

## 3. Backend — Batch Setup Endpoint

- [x] 3.1 Define `SetupDocksCommand` with payload shape: `{ docks: [{ name, slips: [{ name, maxLength, maxBeam, maxDraft, slipType, hasElectric, electric, hasWater, hasPumpOut, isCovered, isIndoor, amenities }] }] }`
- [x] 3.2 Implement `SetupDocksCommandHandler` — validates marina is draft, deletes existing draft docks/slips, inserts new tree in a single transaction; updates `Marina.SetupStep`
- [x] 3.3 Add `PUT /marinas/{id}/setup/docks` controller action; require marina `Owner` or `Manager` membership; return 409 if `IsSetupComplete = true`
- [x] 3.4 Add FluentValidation for `SetupDocksCommand` (dock names unique, slip names unique within dock, dimensions positive)
- [x] 3.5 Update `PUT /marinas/{id}` (marina profile PATCH) to also accept and persist `SetupStep` for step-tracking from the wizard

## 4. Backend — Slip Amenity Fields on Existing Endpoints

- [x] 4.1 Update `SlipDto` / slip response shape to include `hasPumpOut`, `isCovered`, `isIndoor`, `amenities`
- [x] 4.2 Update slip create (`POST /marinas/{id}/slips`) and update (`PATCH /marinas/{id}/slips/{slipId}`) payloads to accept the new amenity fields
- [x] 4.3 Regenerate `src/MyMarina.Web/src/api/schema.d.ts` by running `npm run generate-api` against the updated running API

## 5. Frontend — Shared Utilities

- [x] 5.1 Extract the Leaflet map from `SearchPage.tsx` into a reusable `MapPicker` component (`src/MyMarina.Web/src/components/MapPicker.tsx`) accepting `lat`, `lng`, `onPositionChange` props with a single draggable marker
- [x] 5.2 Implement `nominatimGeocode(address)` utility with progressive fallback chain: full address → city+state+zip → city+state → state; return `{ lat, lng, precisionLabel }` or `null`; include required `User-Agent` header
- [x] 5.3 Implement naming convention generators (`src/MyMarina.Web/src/utils/namingConventions.ts`): `generateDockName` and `generateSlipName` as pure functions over discriminated union types; include `Lettered`, `Numbered`, `Manual` (dock) and `PerDockReset`, `PerDockGlobal`, `Sequential`, `Manual` (slip)
- [x] 5.4 Implement `useWizardDraft(marinaId)` hook (`src/MyMarina.Web/src/hooks/useWizardDraft.ts`) — reads/writes localStorage under `marina-setup-{marinaId}`, compares timestamps with backend on load, exposes `save()` and `clear()` helpers

## 6. Frontend — Home Page Updates

- [x] 6.1 Update `MyMarinaDto` type usage in `HomePage.tsx` to consume `isSetupComplete` and `setupStep`
- [x] 6.2 Add draft card variant to `MarinaCard` component — shows "Draft" badge, "Continue setup →" link to `/marina/{id}/setup`, and "Delete draft" button with confirmation dialog
- [x] 6.3 Add dismissible setup banner to `HomePage` shown when user has zero non-draft marinas; dismiss stores flag in localStorage

## 7. Frontend — Wizard: Step 1 (Marina Profile)

- [x] 7.1 Add `/marina/{id}/setup` route to `App.tsx` routing and create `MarinaSetupWizardPage.tsx` shell with step state management
- [x] 7.2 Update `/marina/new` handler in `App.tsx` to render the wizard shell at step 1 (create draft marina on submit, redirect to `/marina/{id}/setup`)
- [x] 7.3 Implement Step 1 form: marina name, type (Commercial/YachtClub/PrivateCommunity), street address, city, state, zip, phone, email, website (optional), timezone, description (optional); Zod schema validation
- [x] 7.4 On Step 1 submit: call `POST /auth/signup/marina` (or equivalent) with `isSetupComplete: false`; redirect to `/marina/{id}/setup` at step 2

## 8. Frontend — Wizard: Step 2 (GPS Location)

- [x] 8.1 Implement Step 2 layout: address summary, "Locate on map" button, `MapPicker` component, lat/long display fields (read-only, updated by pin)
- [x] 8.2 Wire "Locate on map" button to `nominatimGeocode`; on result fly the `MapPicker` to coordinates and show precision label; on null result show informational message
- [x] 8.3 On Step 2 "Next": PATCH marina with `latitude`, `longitude`, and `setupStep: 2`; sync localStorage

## 9. Frontend — Wizard: Step 3 (Dock Structure Builder)

- [x] 9.1 Implement "Do you have docks?" yes/no toggle; "No" skips to Step 5 (publish)
- [x] 9.2 Implement dock count input and dock naming convention selector (Lettered/Numbered/Manual) with config params (prefix, suffix, startAt); preview generated dock names live
- [x] 9.3 Implement slip count input (uniform for all docks, or per-dock toggle); slip naming convention selector (PerDockReset/PerDockGlobal/Sequential/Manual) with config params; preview generated slip names live
- [x] 9.4 Implement per-dock default amenities/dimensions section: MaxLength, MaxBeam, MaxDraft, SlipType, HasElectric, Electric, HasWater, HasPumpOut, IsCovered, IsIndoor, custom tag add/remove input
- [x] 9.5 On Step 3 "Next": generate full dock+slip structure using naming convention generators and dock defaults; store in localStorage; call `PUT /marinas/{id}/setup/docks` with generated payload; update `setupStep: 3`

## 10. Frontend — Wizard: Step 4 (Preview & Adjust)

- [x] 10.1 Fetch current draft dock+slip tree from backend on step load (to reflect any backend state); merge with localStorage state using timestamp comparison
- [x] 10.2 Render dock groups as collapsible sections; each dock header shows dock name, slip count, and "Edit all slips" + "Remove dock" actions
- [x] 10.3 Implement bulk-edit dialog for a dock: change any slip field for all slips in the dock at once; persist changes to localStorage and debounce-sync to backend via individual PATCH calls
- [x] 10.4 Implement inline cell editing in the slip table: click a cell to edit, commit on blur/Enter; persist to localStorage immediately, sync to backend on debounce
- [x] 10.5 Implement "Add slip" action per dock: append slip with dock defaults and next generated name; persist to backend
- [x] 10.6 Implement "Remove slip" action: delete row from table and call DELETE endpoint
- [x] 10.7 Show a persistent "Save progress" button in the wizard header; on click flush localStorage state to backend and show confirmation toast

## 11. Frontend — Wizard: Step 5 (Review & Publish)

- [x] 11.1 Implement summary display: marina name, type, address, dock count, total slip count
- [x] 11.2 Add "List on marketplace" toggle defaulting to OFF with educational copy explaining immediate boater visibility
- [x] 11.3 On "Finish setup" submit: call PATCH marina with `isSetupComplete: true` and `isListed` per toggle; clear localStorage draft; redirect to `/marina/{id}` dashboard

## 12. Testing & Polish

- [x] 12.1 Add unit tests for all naming convention generators (all conventions, edge cases: 0 docks, 26+ lettered docks, padZeros)
- [x] 12.2 Add integration test: `PUT /marinas/{id}/setup/docks` — successful replace, 409 on active marina, rollback on error
- [x] 12.3 Add integration test: `DELETE /marinas/{id}` — succeeds on draft, 409 on active
- [x] 12.4 Add integration test: draft marina excluded from slip search results
- [x] 12.5 Verify demo seed CI test still passes after amenity column additions
- [x] 12.6 Capture Playwright screenshots of the wizard and update `src/MyMarina.Marketing/public/screenshots/` using the `playwright-cli` skill
