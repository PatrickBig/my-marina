## Purpose

Defines the public slip and marina search endpoints and the boater-facing discovery UI. Covers the two-step rollup→slip-list flow, bounding-box geographic filtering, vessel-driven dimension filters, price-range filtering against resolved pricing, and how demo-tenant marinas are exposed or excluded.

## Requirements

### Requirement: Marina rollup search by map viewport

The system SHALL provide a public endpoint that returns marinas matching the boater's filter criteria, aggregated as marina-level rollup rows. The geographic search area SHALL be defined by an axis-aligned bounding box (`north`, `south`, `east`, `west`) supplied by the caller. No radius parameter is accepted.

The endpoint SHALL be `GET /marinas/search`, accessible anonymously, and SHALL return rows shaped as follows: `MarinaId`, `MarinaName`, `City`, `State`, `Latitude`, `Longitude`, `AvailableCount` (integer count of slips at that marina that match every filter), `InstantBookAvailable` (boolean), `DistanceMilesFromCenter` (double, computed from the bounding-box centroid), `PhotoUrl` (string or null — null until the photo upload feature ships), `HasPumpOut` (boolean — true if any matching slip has `HasPumpOut = true`), `HasElectric` (boolean — true if any matching slip has `ElectricAmpsAvailable > 0`), `IsAnyCovered` (boolean — true if any matching slip has `IsCovered = true`).

Filters supplied by the caller SHALL include: `listingKind`, `arrivesAt`/`departsAt` (transient) or `desiredStart`/`leaseTerm` (lease), `vesselLength`/`vesselBeam`/`vesselDraft`, `slipType`, `hasElectric`, `hasWater`, `instantBookOnly` (boolean — when true, only marinas with at least one instant-book slip are included), `hasPumpOut` (boolean — when true, only marinas with at least one pump-out slip are included), `isAnyCovered` (boolean — when true, only marinas with at least one covered slip are included), and optionally `priceMin`/`priceMax`. Filters SHALL be applied at the slip level before aggregation; only slips that match every filter SHALL contribute to a marina's `AvailableCount`.

The `priceMin` and `priceMax` filters SHALL only be applied when `listingKind` is also provided. For `listingKind=Transient`, the price filter applies to per-night rates. For `listingKind=Lease`, the price filter applies to per-period rates. A slip passes the price filter if ANY of its matching availability windows OR its default rate falls within the specified range — an existence check, not an aggregation. If `priceMin` or `priceMax` is supplied without `listingKind`, the server SHALL return a `400 Bad Request`.

Marinas with `AvailableCount = 0` SHALL be excluded from the response. Demo-tenant marinas SHALL be excluded unless `IUserContext.IsDemo` is `true`. Aggregation SHALL be expressed as a single grouped database query (no in-memory `GroupBy` over thousands of slip rows).

The aggregate boolean fields (`HasPumpOut`, `HasElectric`, `IsAnyCovered`) SHALL be computed as `BOOL_OR` aggregate expressions in the same `GROUP BY` pass as `AvailableCount` — no additional query passes. The amenity filter parameters (`instantBookOnly`, `hasPumpOut`, `isAnyCovered`) SHALL be applied as `EXISTS` subquery predicates, not by loading all slip rows into memory.

Price fields (`MinPricePerNight`, `MaxPricePerNight`, `RateKind`) SHALL NOT be returned in the marina rollup response. Prices are available in the per-marina slip list (`GET /marinas/{id}/slips/search`).

#### Scenario: Marina rollup with multiple matching slips

- **WHEN** a boater searches a bounding box covering Annapolis with `listingKind=Transient`, `arrivesAt=2026-07-04`, `departsAt=2026-07-08`, and a 45'×16'×6' boat
- **AND** Sunset Point Marina has 24 active slips that fit the boat with open transient windows covering those dates
- **AND** at least one of those slips has `InstantBook = true`, `HasPumpOut = true`, and `ElectricAmpsAvailable > 0`
- **THEN** the response includes a row for Sunset Point with `AvailableCount = 24`, `InstantBookAvailable = true`, `HasPumpOut = true`, `HasElectric = true`
- **AND** the row does NOT contain `MinPricePerNight`, `MaxPricePerNight`, or `RateKind` fields
- **AND** `PhotoUrl` is null (until photo upload feature ships)

#### Scenario: Instant-book filter excludes marinas without instant-book slips

- **WHEN** a boater searches with `instantBookOnly=true`
- **AND** Harbor Cove has 5 available slips, none with `InstantBook = true`
- **THEN** Harbor Cove is excluded from the response

#### Scenario: Pump-out filter excludes marinas without pump-out

- **WHEN** a boater searches with `hasPumpOut=true`
- **AND** Eastport Yacht Club has 8 available slips, none with `HasPumpOut = true`
- **THEN** Eastport Yacht Club is excluded from the response

#### Scenario: Amenity aggregate fields computed in single pass

- **WHEN** the marina rollup query executes
- **THEN** `HasPumpOut`, `HasElectric`, and `IsAnyCovered` are computed as BOOL_OR aggregates in the same GROUP BY pass as `AvailableCount`
- **AND** no separate aggregation query is issued to the database

#### Scenario: Price range filter — matching slips included

- **WHEN** a boater searches with `listingKind=Transient`, `priceMin=100`, `priceMax=200`
- **AND** Sunset Point has slips with transient window prices of $150/night and $250/night
- **THEN** Sunset Point IS included in the response (at least one slip falls within $100–$200)
- **AND** `AvailableCount` reflects only the slips that match ALL filters including price

#### Scenario: Price range filter — no matching slips excludes marina

- **WHEN** a boater searches with `listingKind=Transient`, `priceMax=50`
- **AND** all of Harbor View Marina's slips have per-night rates above $50
- **THEN** Harbor View Marina is excluded from the response

#### Scenario: Price filter without listing kind returns 400

- **WHEN** a caller supplies `priceMin=100` but omits `listingKind`
- **THEN** the response is `400 Bad Request`

#### Scenario: Marina excluded when no slips match

- **WHEN** Harbor Town has 50 slips but none fit a 60'×20'×8' boat
- **THEN** the response excludes Harbor Town entirely

#### Scenario: Demo tenant excluded for real users

- **WHEN** a non-demo boater searches a bounding box that includes a demo-tenant marina
- **THEN** the demo marina is excluded from the response

#### Scenario: Demo tenant included for demo session

- **WHEN** an anonymous demo session (`IUserContext.IsDemo = true`) searches the same bounding box
- **THEN** the demo marina is included in the response

#### Scenario: Bounding box defines the search area

- **WHEN** the caller supplies `north=39.0`, `south=38.9`, `east=-76.4`, `west=-76.6`
- **THEN** only marinas whose `(Latitude, Longitude)` fall within the box are returned
- **AND** no `radiusMiles` parameter is accepted on this endpoint

### Requirement: Slip search scoped to a single marina

The system SHALL provide a public endpoint that returns matching slips at a single marina. The endpoint SHALL be `GET /marinas/{id}/slips/search`, accessible anonymously. It SHALL accept the same filters as the marina rollup endpoint EXCEPT it SHALL NOT accept any geographic parameters (no bounding box, no radius). All matching slips at the named marina SHALL be returned.

The response SHALL use the existing `SlipSearchResultDto` shape (one row per slip).

#### Scenario: All matching slips returned for the marina

- **WHEN** a boater clicks Sunset Point Marina from the rollup view
- **AND** the slip-list endpoint is called with the same `listingKind`, dates, and vessel filters
- **THEN** every active slip at Sunset Point Marina that matches those filters is returned
- **AND** no geographic filter is applied

#### Scenario: Vessel-fit filter still applies

- **WHEN** the request specifies `vesselLength=45`, `vesselBeam=16`, `vesselDraft=6`
- **THEN** only slips with `MaxLength >= 45 AND MaxBeam >= 16 AND MaxDraft >= 6` are returned

#### Scenario: Marina not found returns 404

- **WHEN** the caller supplies a `{id}` that does not exist
- **THEN** the response is `404 Not Found`

#### Scenario: Demo marina visible only in demo session

- **WHEN** a non-demo boater requests slips at a demo-tenant marina
- **THEN** the response is `404 Not Found` (the marina is not visible to them)
- **AND** when an `IUserContext.IsDemo = true` session requests the same marina, the slip list is returned normally

### Requirement: Removal of legacy flat slip search

The system SHALL NOT expose `GET /slips/search`. The previous flat per-slip search endpoint and its handler SHALL be removed.

#### Scenario: Old endpoint returns 404

- **WHEN** any caller invokes `GET /slips/search`
- **THEN** the response is `404 Not Found` (route does not exist)

### Requirement: Vessel-driven dimension filters in the boater UI

The boater-facing search UI SHALL allow an authenticated user to select one of their `Vessel` records as the source of `vesselLength`, `vesselBeam`, and `vesselDraft` filters. Manual numeric entry SHALL remain available for anonymous users and as an opt-out for authenticated users.

When a user selects a vessel, the UI SHALL persist the selected `VesselId` to browser localStorage under the key `mymarina:lastSelectedVesselId`. On subsequent visits, the UI SHALL initialize the vessel selector to that id if it still corresponds to one of the user's vessels; otherwise it SHALL select the most recently created vessel (`CreatedAt DESC`).

The UI SHALL NOT depend on a `Vessel.IsPrimary` or `Vessel.LastUsedAt` field. No backend or schema change SHALL be required to support default selection.

#### Scenario: Authenticated user with a primary boat

- **WHEN** an authenticated user with three vessels visits the search page for the first time
- **AND** their vessels were created on 2026-01-10, 2026-03-15, and 2026-05-01
- **THEN** the vessel selector defaults to the 2026-05-01 vessel
- **AND** its dimensions are sent as `vesselLength`/`vesselBeam`/`vesselDraft` on search

#### Scenario: Selection persists across sessions

- **WHEN** the user changes the selector to a different vessel
- **AND** later returns to the search page in a new browser session
- **THEN** the selector initializes to the most recently selected vessel

#### Scenario: Stored vessel id no longer exists

- **WHEN** localStorage holds a `lastSelectedVesselId` that the user has since deleted
- **THEN** the selector falls back to the most recently created vessel
- **AND** the localStorage value is overwritten with that vessel's id

#### Scenario: Anonymous user sees manual dimension inputs

- **WHEN** an unauthenticated visitor opens the search page
- **THEN** the vessel selector is not shown
- **AND** the manual `LOA`, `Beam`, and `Draft` inputs are visible

#### Scenario: Authenticated user opts to enter dimensions manually

- **WHEN** an authenticated user selects "use different dimensions"
- **THEN** the manual inputs become visible
- **AND** values entered there override any selected vessel for that search

### Requirement: Map viewport is the search area; no radius input

The boater-facing search UI SHALL use the current map viewport as the search-area definition for marina rollup queries. The UI SHALL NOT expose a radius input control. The map's visible bounds SHALL be converted to `north`/`south`/`east`/`west` parameters and passed to `GET /marinas/search`.

#### Scenario: Geocoded location auto-runs the search

- **WHEN** the user enters "Annapolis" and submits the location field
- **THEN** the UI geocodes the text, centers the map at the resulting coordinate at default zoom, and immediately runs `GET /marinas/search` against the resulting viewport
- **AND** results appear without further user action

#### Scenario: Geolocation auto-runs the search

- **WHEN** the user clicks the geolocation button (📍) and grants permission
- **THEN** the map centers on the user's coordinates at default zoom and immediately runs the search

#### Scenario: Map pan or zoom requires explicit re-search

- **WHEN** the user pans or zooms the map after a search has run
- **THEN** a "Search this area" button appears
- **AND** results are NOT updated until the user clicks the button
- **AND** clicking the button re-runs the search using the current viewport bounding box

#### Scenario: Radius input is not present

- **WHEN** the search page is loaded
- **THEN** no `Radius (mi)` input control is shown anywhere on the page

### Requirement: Two-step boater discovery flow with shareable URLs

The boater-facing slip search SHALL be a two-step flow with distinct routes:

1. `/search` — marina rollup view, default landing.
2. `/search/marinas/:marinaId` — slips-at-marina view, reachable by clicking a marina row from step 1.

Filter state (dates, vessel selection, listing kind, slip-type/amenity filters) SHALL be carried through the URL search params so that step-2 URLs are independently shareable and reload with the same filter context.

#### Scenario: Click-through preserves filters

- **WHEN** the user runs a marina search with `listingKind=Transient`, dates 2026-07-04 to 2026-07-08, and vessel "My Catalina 30"
- **AND** clicks the Sunset Point Marina row
- **THEN** the browser navigates to `/search/marinas/{sunsetPointId}` with the date and filter params preserved in the URL
- **AND** the slip list shows slips matching those same filters

#### Scenario: Direct visit to a step-2 URL

- **WHEN** a user opens `/search/marinas/{marinaId}?listingKind=Transient&arrivesAt=2026-07-04&departsAt=2026-07-08`
- **THEN** the page loads the slips-at-marina view for that marina with the supplied filters applied
- **AND** the back button returns to the marina rollup view

#### Scenario: Back navigation to marina list

- **WHEN** the user is on `/search/marinas/{marinaId}` and clicks "Back to marinas" or the browser back button
- **THEN** the marina rollup view is restored with the same filters and map viewport
