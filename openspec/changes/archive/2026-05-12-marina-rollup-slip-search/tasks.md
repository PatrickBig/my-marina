## 1. Discovery

- [x] 1.1 Locate existing user-vessels endpoint (`MyMarina.Application.Vessels`) — confirm it returns `VesselId`, dimensions, and `CreatedAt`. If insufficient, add a minimal `GET /vessels/mine` returning the fields the selector needs.
- [x] 1.2 Confirm DB indexes exist on `Slip.MarinaId`, `Slip.Latitude`, `Slip.Longitude`, `Slip.Status`. Add missing indexes via an EF migration (`dotnet ef migrations add AddSlipSearchIndexes` if needed).
- [x] 1.3 Skim `SlipSearchController` and `SearchSlipsQueryHandler` to inventory every filter currently honored — vessel-fit, dates, listing kind, slip type, electric, water, lease term, demo exclusion. The new endpoints must preserve all of them.

## 2. Backend — DTOs and query types

- [x] 2.1 Add `MarinaSearchResultDto` in `src/MyMarina.Application/Search/SlipSearchDtos.cs` with the fields specified in proposal.md.
- [x] 2.2 Add `SearchMarinasQuery` record in `src/MyMarina.Application/Search/SlipSearchQueries.cs`. Fields: `North`, `South`, `East`, `West` (decimals), `ListingKind`, `ArrivesAt?`, `DepartsAt?`, `LeaseTerm?`, `VesselLength?`, `VesselBeam?`, `VesselDraft?`, `SlipType?`, `HasElectric?`, `HasWater?`, `Page`, `PageSize`, `IncludeDemo`.
- [x] 2.3 Add `SearchSlipsAtMarinaQuery` record. Fields: `MarinaId` plus every non-geographic filter from `SearchSlipsQuery`. No `Latitude`/`Longitude`/`RadiusMiles`.

## 3. Backend — query handlers

- [x] 3.1 Implement `SearchMarinasQueryHandler` in `src/MyMarina.Infrastructure/Search/`. Strategy: a single grouped EF query — filter `Slips` by bounding box + vessel-fit + amenity + slip-type + demo + active + assignment/reservation/window eligibility (mirror existing logic from both transient and lease paths in `SearchSlipsQueryHandler`); `GroupBy(s => s.MarinaId)`; project to `MarinaSearchResultDto` with `Count()`, `Min(price)`, `Max(price)`, mixed-rate detection (`Distinct(rateKind).Count() > 1 ? "Mixed" : ...`), and `Any(window.InstantBook || slip.DefaultTransientBaseRate != null)` for `InstantBookAvailable`. Compute `DistanceMilesFromCenter` per row using `GeoHelper.HaversineDistanceMiles` from `((North+South)/2, (East+West)/2)`. Sort by `AvailableCount DESC, DistanceMilesFromCenter ASC`. Page/PageSize applied last.
- [x] 3.2 Implement `SearchSlipsAtMarinaQueryHandler`. Reuse the slip-level filter logic from `SearchSlipsQueryHandler` (consider extracting shared filter predicates into a small helper class to avoid duplication). Scope by `s.MarinaId == query.MarinaId`. Return `IReadOnlyList<SlipSearchResultDto>`.
- [x] 3.3 Wire both handlers via Scrutor scanning (verify `MyMarina.Infrastructure.DependencyInjection` registers them automatically; if not, add explicit registrations).
- [x] 3.4 Delete `SearchSlipsQueryHandler.cs`. Remove any references.

## 4. Backend — controllers

- [x] 4.1 Create `src/MyMarina.Api/Controllers/MarinaSearchController.cs`. Two actions:
  - `[HttpGet("marinas/search")] [AllowAnonymous]` calling `SearchMarinasQuery` handler. Validate at least one of the bbox bounds; default page size 20, max 50.
  - `[HttpGet("marinas/{id:guid}/slips/search")] [AllowAnonymous]` calling `SearchSlipsAtMarinaQuery` handler. Return 404 if marina id not found OR if marina is demo and `IUserContext.IsDemo == false`.
- [x] 4.2 Remove `SlipSearchController.Search` action. Keep the `GetDetail` action; consider renaming the controller to `SlipDetailController` or moving the action into a unified `SlipsController` — pick the simplest move that avoids confusion.
- [x] 4.3 Add `[ProducesResponseType]` annotations on both new actions for OpenAPI typing.

## 5. Backend — tests

- [x] 5.1 Integration test: marina rollup returns aggregated counts + price range when multiple slips at one marina match the filters.
- [x] 5.2 Integration test: marina with no matching slips is excluded.
- [x] 5.3 Integration test: bounding-box filter excludes marinas outside the box.
- [x] 5.4 Integration test: demo-tenant marina excluded for non-demo session, included for demo session.
- [x] 5.5 Integration test: `RateKind = "Mixed"` when matching slips span PerFoot and Flat rates.
- [x] 5.6 Integration test: per-marina slip search returns only that marina's matching slips; ignores any geographic params if accidentally passed.
- [x] 5.7 Integration test: per-marina slip search returns 404 for unknown marina id.
- [x] 5.8 Integration test: legacy `GET /slips/search` returns 404 (route removed).
- [x] 5.9 Run full `dotnet test` suite; fix any regressions.

## 6. Frontend — API regeneration

- [x] 6.1 Run `dotnet watch --project src/MyMarina.Api` and `npm run generate-api` from `src/MyMarina.Web/` to regenerate `src/MyMarina.Web/src/api/schema.d.ts`. Do not hand-edit.
- [x] 6.2 Add typed wrappers in `src/MyMarina.Web/src/api/api.ts`: `searchMarinas(...)`, `searchSlipsAtMarina(marinaId, ...)`. Remove the old `searchSlips` wrapper.

## 7. Frontend — vessel selector

- [x] 7.1 Add `VesselSelector` component in `src/MyMarina.Web/src/components/`. Props: `value: string | null`, `onChange(vesselId | null, dimensions)`, `onUseDifferentDimensions()`. Internally fetches the user's vessels via TanStack Query (only when authenticated). Renders a dropdown for authenticated users with vessels; renders nothing for anonymous users.
- [x] 7.2 Implement default-selection logic: read `localStorage.getItem('mymarina:lastSelectedVesselId')`, validate against the fetched list, fall back to most-recently-created. On change, write to localStorage.
- [x] 7.3 Add manual-dimension inputs as a separate component (`VesselDimensionInputs`) reusable for anonymous and opt-out flows.

## 8. Frontend — routes and pages

- [x] 8.1 Restructure `src/MyMarina.Web/src/pages/SearchPage.tsx` into the marina-rollup view. Strip date/vessel/listing-kind state into URL search params via TanStack Router (search-param schema with Zod).
- [x] 8.2 Create `MarinaSlipsPage` at `src/MyMarina.Web/src/pages/MarinaSlipsPage.tsx`. Route: `/search/marinas/:marinaId`. Reads filters from URL. Calls `searchSlipsAtMarina`. Renders the slip list (reuse the existing slip-row UI from the old `SearchPage`).
- [x] 8.3 Wire both routes into the TanStack Router tree. Confirm browser back/forward and direct URL load both work.

## 9. Frontend — map viewport search

- [x] 9.1 Add a `useMapViewportBounds()` hook (or `MapViewportBounds` helper component) that subscribes to react-leaflet `moveend` and `zoomend` events and exposes the current `(north, south, east, west)`.
- [x] 9.2 On first render after geocode or geolocation, auto-run `searchMarinas` against the resulting viewport.
- [x] 9.3 After any `moveend`/`zoomend` triggered by user interaction, render a "Search this area" overlay button on the map. Clicking invokes `searchMarinas` with the current bounds and dismisses the button. The overlay should not appear on the initial programmatic recenter (geocode / geolocation).
- [x] 9.4 Remove the `Radius (mi)` input from the search form. Remove `radiusMiles` state. Confirm no references remain.

## 10. Frontend — marina rollup UI

- [x] 10.1 Render the marina list as a table or card grid (one row per marina) showing name, city/state, available count, price range or "from $X" for `Mixed`, and instant-book badge. Sort UI hint: "Most options" (default) / "Closest" / "Lowest price".
- [x] 10.2 Render marina pins on the map with available-count badges. Hovering a row highlights the corresponding pin and vice versa.
- [x] 10.3 Clicking a marina row navigates to `/search/marinas/:marinaId` carrying current filters in URL params.

## 11. Frontend — per-marina slip page UI

- [x] 11.1 Display marina name, city/state, and a "Back to marinas" link (preserves the prior viewport when going back).
- [x] 11.2 Render the slip list using the existing slip-row layout. The map shows the marina pin only (no slip-level visualization in this change).
- [x] 11.3 Clicking a slip row navigates to `/slips/{slipId}` (existing slip-detail page; unchanged).

## 12. Demo data

- [x] 12.1 In `MyMarina.Infrastructure/Demo/DemoSeedScript.cs`, ensure the demo tenant has at least three marinas in the same metro area (e.g., Annapolis) so the rollup view is meaningful.
- [x] 12.2 Ensure at least one demo marina has a mix of `PerFoot` and `Flat` slip rates so `RateKind = "Mixed"` is exercised.
- [x] 12.3 Ensure the existing per-entity-type seed-coverage CI test still passes.

## 13. Docs and screenshots

- [x] 13.1 Rewrite the "Discovery & search" section of `docs/marketplace.md` to describe the two-step flow, viewport bounding-box search, and vessel selector. Remove references to `radiusMiles` and the flat-list behavior.
- [ ] 13.2 Use the `playwright-cli` skill to capture screenshots of: (a) the marina rollup view with map + list, (b) the "Search this area" button after a pan, (c) the per-marina slip list. Save to `src/MyMarina.Marketing/public/screenshots/` and reference from `ScreenshotsSection`.

## 14. Final validation

- [x] 14.1 `dotnet build` and `dotnet test` from repo root — all green.
- [x] 14.2 `npm run build` from `src/MyMarina.Web/` — clean build.
- [x] 14.3 Manual smoke test in the browser: geocode a city → marina list appears → pan map → button appears → click → re-search → click marina → slip list → click slip → detail page. Both anonymous and authenticated paths.
- [x] 14.4 Verify localStorage `mymarina:lastSelectedVesselId` round-trips across page reloads for an authenticated user.
- [x] 14.5 Run `openspec validate marina-rollup-slip-search --strict` and resolve any findings.
