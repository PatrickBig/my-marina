## MODIFIED Requirements

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
