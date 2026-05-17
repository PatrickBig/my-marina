## Context

The current slip search (`GET /slips/search` + `SearchPage.tsx`) returns a flat, point-radius list. It works for sparse data but breaks down when several marinas in range each have hundreds of slips: results become indistinguishable, marinas can't be compared at a glance, and no quick "this marina has 24 options matching my criteria" signal exists.

Three orthogonal pain points have surfaced together and are addressed in one change because they share a UX restructure:

1. **Result organization.** Boaters mentally pick a marina (location, price band, instant-book) before picking a slip. The flat list inverts this.
2. **Vessel dimension entry.** `Vessel` records exist per user but aren't used by search; users re-type LOA/beam/draft on every search.
3. **Search-area control.** A static `Radius (mi)` number doesn't match the visible map. Users think in terms of "what I see," not "miles from a centroid."

Existing entities (`Slip`, `Marina`, `Vessel`, `AvailabilityWindow`, `SlipAssignment`, `Reservation`) are sufficient — no schema changes. The work is API + UX restructuring.

Constraints:
- Public/anonymous access must continue (`[AllowAnonymous]`) for both new endpoints — boaters browse before signing in.
- Demo tenant exclusion (`IUserContext.IsDemo`) carries through unchanged.
- "Built to scale" mandate (CLAUDE.md): aggregate at the DB level, not in memory.
- Frontend uses TanStack Router, TanStack Query, react-leaflet — already in the stack.

## Goals / Non-Goals

**Goals:**
- Replace one flat search with a two-step marina-first flow that scales to marinas with hundreds of slips.
- Make the map viewport the single source of truth for the search area; surface it as the obvious control.
- Use the boater's `Vessel` records to populate fit filters, with manual entry as a fallback for anonymous users.
- Single grouped DB query for marina rollups — no N+1, no in-memory aggregation over thousands of slips.
- Preserve current filter semantics (vessel-fit, dates, listing kind, slip type, amenities, lease term, demo-tenant exclusion) without behavioral drift.

**Non-Goals:**
- Marina aerial / per-slip dock visualization in step 2 — explicitly deferred to the existing `marina-map-visualization` change.
- Drag-and-drop slip arrangement for operators — deferred.
- Search response caching (Redis / `IMemoryCache`) — flagged as a future optimization but out of scope here.
- Auto-search on every map idle event — using explicit "Search this area" button instead.
- Adding `Vessel.IsPrimary` or `Vessel.LastUsedAt` fields — `CreatedAt DESC` + localStorage suffices.
- Removing or changing `GET /slips/{id}` (slip detail) — out of scope.

## Decisions

### 1. Hard two-step flow (drill-in pages) vs. collapsible groups

**Chosen:** Hard two-step. Step 1 = marina list. Click → step 2 = slips at that marina (separate route, separate page).

**Why:** Collapsing 500 slips inside an accordion is impractical scrolling and breaks page-anchored map state. Boaters typically commit to a marina before reviewing slips, so paying a click to drill in matches the decision sequence. URLs become directly shareable per step (`/search` for marinas, `/search/marinas/{id}` for slips at a marina).

**Alternatives considered:** Collapsible accordion groups (rejected — scroll burden); flat list with marina-grouping headers (rejected — preserves the unreadability problem).

### 2. "Search this area" button vs. auto-search-on-pan

**Chosen:** Explicit button that appears after the user pans/zooms the map. Clicking re-runs the marina search against the current viewport bounding box.

**Why:** Auto-search-on-idle is chatty (many fetches per pan), gives the user no commitment point, and breaks if the user is mid-exploration. The button matches the convention boaters know from real-estate apps (Zillow, Redfin), and is more deliberate.

**Alternatives considered:** Debounced auto-search at 400ms idle (rejected — too chatty, not user-controlled); always-search-on-zoom-only (rejected — inconsistent).

### 3. Drop the `Radius (mi)` input

**Chosen:** Remove the radius input entirely. The map viewport defines the search area.

**Why:** Two controls for "where am I searching" creates ambiguity (which wins?). The map viewport is visual, intuitive, and the same control as the resulting display — single source of truth. Geolocation 📍 sets the map center; zooming controls extent.

**Alternatives considered:** Keep radius as a constraint within the viewport (rejected — confusing); replace with a "max miles" filter (rejected — same conceptual overlap).

### 4. Drop the bounding-box filter in step 2

**Chosen:** Step 2 (`GET /marinas/{id}/slips/search`) does not accept a bounding box. It returns all matching slips at that marina.

**Why:** A marina is a single point. Filtering its slips by a geographic box around the marina coordinate is meaningless — they're either all in or all out. Vessel-fit, dates, listing kind, slip type, and amenity filters still apply.

### 5. Vessel selector default + persistence

**Chosen:**
1. On page load, read `mymarina:lastSelectedVesselId` from localStorage.
2. Fetch the user's vessels (`GET /vessels` or equivalent — see Decision 6).
3. If localStorage value matches an existing vessel, select it.
4. Otherwise, select the most recently created vessel (`CreatedAt DESC`).
5. On selection change, write the new id to localStorage.
6. Anonymous users (or users opting out via "use different dimensions") see manual LOA/beam/draft inputs.

**Why:** Most boaters have one boat; many have one primary boat. `CreatedAt DESC` is a reasonable proxy for "primary" without adding a `Vessel.IsPrimary` field. localStorage handles the multi-boat / charter case (sticky last selection per browser). No backend or schema change required.

**Alternatives considered:** Add `Vessel.IsPrimary` flag (rejected — premature; the heuristic works); server-side preference storage (rejected — over-engineering for a UI nicety).

### 6. Vessel fetching — reuse existing endpoint

**Chosen:** Use the existing user-vessels endpoint if present; only add a minimal new one if no suitable endpoint exists. Investigate during implementation (`Tasks 1.1`).

**Why:** Avoid duplicating endpoints. The `MyMarina.Application.Vessels` module almost certainly has a "list my vessels" query already.

### 7. Two parallel endpoints (option α) vs. `?groupBy=marina` parameter (option β)

**Chosen:** Two parallel endpoints. `GET /marinas/search` returns `MarinaSearchResultDto[]`; `GET /marinas/{id}/slips/search` returns `SlipSearchResultDto[]`.

**Why:** Cleaner REST shape — each route has one response type. Overloading a single endpoint with conditional response shapes is a maintenance smell and breaks OpenAPI typing.

**Alternatives considered:** `GET /slips/search?groupBy=marina` returning a union type (rejected — overloaded responses, harder to type, worse for consumers).

### 8. Delete `GET /slips/search` rather than deprecate

**Chosen:** Remove `SlipSearchController.Search` entirely. Keep `GET /slips/{id}` (slip detail).

**Why:** Per CLAUDE.md, the product is not live. There are no external consumers to preserve compatibility for. Deprecation overhead is unjustified.

### 9. First geocode auto-runs the search

**Chosen:** When the user types "Annapolis" and submits, geocoding sets the map center + default zoom AND runs the search once. Subsequent map pans/zooms require the user to click "Search this area."

**Why:** Without auto-run on geocode, typing a location feels broken (no results appear). After that initial result, deliberate map exploration is the user's choice.

### 10. Backend bounding-box query — direct, no Haversine refinement

**Chosen:** New marina/slip search endpoints accept `north`, `south`, `east`, `west` directly. No radius parameter. No Haversine post-filtering of results — every slip whose marina coordinate falls in the box is in. Distance from the viewport center is computed once for sorting/display only.

**Why:** Bounding-box-only is faster and matches what the user sees on screen. The current code converts radius → bounding box → Haversine refinement; the refinement step is unnecessary when the user explicitly defined the box visually. Slight overdraw at the box corners is acceptable (the slips are still inside what the user sees).

### 11. `RateKind = "Mixed"` for cross-rate-kind marinas

**Chosen:** When a marina's matching slips include both `PerFoot` and `Flat` rates, `MarinaSearchResultDto.RateKind = "Mixed"`. UI displays "from $X" rather than "$X – $Y".

**Why:** A `$/ft` rate and a `$/night` rate aren't directly comparable. Showing them in one min/max range would mislead. "from $X" using the lowest-equivalent-night-cost is honest about what's available without conflating units.

### 12. Map technology — keep react-leaflet

**Chosen:** Continue with react-leaflet (already in `SearchPage.tsx`).

**Why:** Already integrated. Supports `<Circle />`, marker layers, and viewport bounds events out of the box. Switching to Mapbox/Google Maps is a bigger change with no proportional benefit for this scope.

### 13. Two routes, distinct pages

**Chosen:**
- `/search` → marina rollup view (default after login or from nav)
- `/search/marinas/:marinaId` → slips-at-marina view, with carry-over of date/vessel/filter params via URL search params

**Why:** Shareability ("send my friend the link to slips at Sunset Point with my dates"). Browser back button works naturally. TanStack Router supports this idiomatically.

## Risks / Trade-offs

- **Map viewport ≠ "near me" for first-time users.** A first geocode of "Annapolis" should land on a sensible default zoom — too zoomed out and rural marinas dominate; too zoomed in and the user misses neighboring marinas. → **Mitigation:** Default zoom 10 (matches current `<MapContainer zoom={10}>`). Tunable.
- **"Search this area" button discoverability.** Users may pan and not realize results are stale. → **Mitigation:** Make button visually prominent — overlay near the top of the map after any pan/zoom, with a subtle "results may be outdated" hint. Consider faintly graying stale pins.
- **Mixed rate kinds may confuse boaters.** "from $X" doesn't tell them whether they'll pay flat or per-foot. → **Mitigation:** On the slip-list (step 2), display per-slip rate kind clearly. Acceptable tradeoff in step 1 — boaters drill in for actual prices anyway.
- **DB performance of grouped marina query.** With ~1M slips and ~10K marinas at scale, a grouped query with subqueries (active assignment exclusion, reservation conflict exclusion) could be slow. → **Mitigation:** Index on `Slip.MarinaId`, `Slip.Latitude`, `Slip.Longitude`, `Slip.Status`. The bounding-box filter pre-narrows aggressively. Observe in load testing; add `IMemoryCache` (5-minute TTL keyed on bbox+filters) post-MVP if needed. Cache decision is explicitly future work, not this change.
- **localStorage selection survives across sessions but not devices.** A boater who picks "My Catalina 30" on phone then opens laptop sees their most-recently-created vessel instead. → **Mitigation:** Acceptable tradeoff. Server-side persistence (or future `Vessel.IsPrimary`) is a follow-up if user feedback warrants.
- **Anonymous users have no Vessel records.** The selector must gracefully degrade to manual dimension inputs. → **Mitigation:** `VesselSelector` component checks auth state; renders manual inputs when unauthenticated or when vessels list is empty.
- **Distance-from-center for marinas at the edge of the bbox.** "Distance" loses meaning when the user has explicitly chosen a viewport. → **Mitigation:** Use distance from viewport center as a secondary sort signal (primary sort = available count DESC, secondary = distance ASC). Don't display distance prominently; offer it as a sort option.

## Migration Plan

This is a UI/API restructuring with no schema changes. Deployment is straightforward:

1. **Backend** ships first: new endpoints live alongside the old `GET /slips/search`. Run integration tests.
2. **Frontend** switches to new endpoints in the same release.
3. **Delete** `SlipSearchController.Search` and `SearchSlipsQueryHandler` after the frontend is updated and the change merges.
4. **Regenerate** `schema.d.ts` (`npm run generate-api`) once the new endpoints are deployed in dev.
5. **Update** `docs/marketplace.md` to describe the new flow.
6. **Refresh** marketing screenshots via `playwright-cli`.

No rollback complexity — the old code is removed in the same PR; reverting the PR restores it.

## Open Questions

- Should the marina-rollup map use clustered pins when many marinas crowd a small area, or always show individual pins? → Recommend: individual pins with badge for available count; defer clustering until a real density problem emerges.
- For lease searches (`listingKind=Lease`), should `MinPricePerNight` / `MaxPricePerNight` be renamed to reflect lease-term pricing? → Recommend: keep field names; `RateKind` and `LeaseTerm`-aware UI explain the unit. Backend stays simpler.
- Vessel selector: show fit warnings inline ("⚠ Your beam exceeds this slip's max")? → Out of scope for this change; surfacing the filter in search already addresses the use case. Detail-page warning could be a follow-up.
