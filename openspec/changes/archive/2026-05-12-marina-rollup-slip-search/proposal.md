## Why

Today's `/slips/search` returns a flat list of individual slips. With marinas that may have hundreds of slips each, results become unreadable — five marinas × 200 slips = 1,000 indistinguishable rows. Boaters typically choose a marina first (location, price band, instant-book availability), then pick a slip within it. The current UX inverts that mental model. Three pain points are being fixed at once: no grouping by marina, manual entry of vessel dimensions despite Vessels being on file, and a static `Radius (mi)` input that doesn't reflect what the user sees on the map.

## What Changes

- **NEW** `GET /marinas/search` — bounding-box query (north/south/east/west), returns marina rollup rows with available-slip count, price min/max, rate-kind summary, instant-book availability, and distance from viewport center. Reuses existing slip-fit/date/listing-kind/amenity filter semantics, aggregating by `MarinaId` in a single grouped DB query.
- **NEW** `GET /marinas/{id}/slips/search` — returns matching slips at a single marina (existing `SlipSearchResultDto` shape). Drops the bounding-box filter; all other filters apply.
- **NEW** `MarinaSearchResultDto` carrying: `MarinaId`, `MarinaName`, `City`, `State`, `Latitude`, `Longitude`, `AvailableCount`, `MinPricePerNight`, `MaxPricePerNight`, `RateKind` (`"Flat"` | `"PerFoot"` | `"Mixed"`), `InstantBookAvailable`, `DistanceMilesFromCenter`.
- **BREAKING** `GET /slips/search` and `SlipSearchController.Search` are deleted. Product is not live; no need to keep legacy.
- **NEW** Frontend two-page flow: marina list (default) → click row → slip list at that marina. Routes captured in URL for shareability.
- **NEW** "Pick a boat" selector replacing manual LOA/beam/draft entry. Logged-in users see their Vessels; default selection comes from `lastSelectedVesselId` in localStorage, falling back to most recently created (`CreatedAt DESC`). Anonymous users (and an opt-out link) keep manual dimension inputs.
- **BREAKING** Frontend `Radius (mi)` input is removed. The map viewport defines the search area: first geocode auto-runs the search; subsequent map pans/zooms surface a "Search this area" button (Zillow pattern) that re-runs the query against the current viewport bounding box.
- Demo seed data updated to ensure rich rollup coverage (multiple marinas in same region with varied rate kinds, including at least one `Mixed`-rate marina).

## Capabilities

### New Capabilities
- `slip-search`: Boater-facing slip discovery — marina rollup search, per-marina slip search, vessel-based filter inputs, and map-viewport-driven search-area selection.

### Modified Capabilities
<!-- None — slip-search is a new capability. The existing `/slips/search` endpoint had no formal spec. -->

## Impact

**Backend:**
- `src/MyMarina.Application/Search/` — new query types (`SearchMarinasQuery`, `SearchSlipsAtMarinaQuery`), new DTO (`MarinaSearchResultDto`). Existing `SearchSlipsQuery` / `SlipSearchResultDto` retained (the latter is reused by step 2).
- `src/MyMarina.Infrastructure/Search/` — new handlers `SearchMarinasQueryHandler`, `SearchSlipsAtMarinaQueryHandler`. Existing `SearchSlipsQueryHandler` deleted.
- `src/MyMarina.Api/Controllers/SlipSearchController.cs` — `Search` action removed; `GetDetail` retained. New `MarinaSearchController` for the two new endpoints (or rename — see design.md).

**Frontend:**
- `src/MyMarina.Web/src/pages/SearchPage.tsx` — restructured into a marina-list page; new `MarinaSlipsPage` for step 2. New `VesselSelector` component. Map utilities for converting viewport to bounding box. "Search this area" overlay.
- `src/MyMarina.Web/src/api/schema.d.ts` — regenerated via `npm run generate-api`.
- New TanStack Router routes (URLs capture state for shareability).

**Demo / docs:**
- `MyMarina.Infrastructure/Demo/DemoSeedScript.cs` — ensure rollup-relevant data (multi-marina region, mixed rate kinds).
- `docs/marketplace.md` "Discovery & search" section — rewrite to reflect new flow.
- Marketing screenshots refreshed via `playwright-cli` (per CLAUDE.md).

**No data-model migration required.** No new entities, no schema changes — purely API + UX restructuring on existing tables.
