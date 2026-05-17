## Requirements

### Requirement: Amenity filter chips on marina rollup search (step 1)

The marina rollup search page (`/search`) SHALL display a row of amenity filter chips below the main filter bar. The chips SHALL be: **Instant Book**, **Electric**, **Pump-out**, **Covered**. Each chip is a toggle — inactive by default, active when clicked.

Active chips SHALL be submitted as boolean query parameters to `GET /marinas/search` on every search trigger (initial load, form submit, "Search this area" button). The parameter names SHALL be `instantBookOnly`, `hasElectric`, `hasPumpOut`, `isAnyCovered` respectively.

Chips MAY be toggled independently. Multiple active chips are combined with AND semantics — a marina only appears if it has at least one available slip satisfying all active chip filters simultaneously.

#### Scenario: No chips active — no filter applied

- **WHEN** the user searches with no filter chips active
- **THEN** the request to `GET /marinas/search` omits `instantBookOnly`, `hasElectric`, `hasPumpOut`, and `isAnyCovered` parameters
- **AND** all marinas with matching available slips are returned regardless of amenities

#### Scenario: Single chip active — filters results

- **WHEN** the user activates the "Pump-out" chip and runs a search
- **THEN** the request includes `hasPumpOut=true`
- **AND** only marinas with at least one available slip that has `HasPumpOut = true` are returned

#### Scenario: Multiple chips active — AND semantics

- **WHEN** the user activates "Instant Book" and "Electric" chips
- **THEN** the request includes `instantBookOnly=true&hasElectric=true`
- **AND** only marinas with at least one available slip that satisfies BOTH instant book AND has electric are returned

#### Scenario: Chips persist through "Search this area"

- **WHEN** the user has activated the "Covered" chip and then pans the map
- **AND** clicks "Search this area"
- **THEN** the new search request still includes `isAnyCovered=true`

---

### Requirement: Marina rollup cards display amenity badge pills

Marina rollup cards in the step-1 result list SHALL display amenity badge pills derived from the API response fields `hasPumpOut`, `hasElectric`, `isAnyCovered`. A badge SHALL render only when the corresponding field is `true` in the response. `InstantBookAvailable = true` already renders an "Instant" badge and SHALL continue to do so.

Additionally, each marina card SHALL reserve a fixed-size image slot. When the `photoUrl` field in the response is non-null, the slot SHALL render an `<img>` with `object-cover` sizing. When `photoUrl` is null, the slot SHALL render a CSS gradient placeholder (ocean blue to sea-foam teal) with a centered ⚓ character.

#### Scenario: Card with all amenities shows all badges

- **WHEN** the rollup response for Sunset Point includes `instantBookAvailable=true`, `hasElectric=true`, `hasPumpOut=true`, `isAnyCovered=true`
- **THEN** the card displays four badge pills: "Instant", "Electric", "Pump-out", "Covered"

#### Scenario: Card with no amenities shows no badges

- **WHEN** the rollup response for Harbor Cove includes `instantBookAvailable=false`, `hasElectric=false`, `hasPumpOut=false`, `isAnyCovered=false`
- **THEN** no amenity badge pills appear on that card

#### Scenario: Photo placeholder renders when photoUrl is null

- **WHEN** a marina card has `photoUrl = null`
- **THEN** the image slot renders a gradient placeholder with an ⚓ character
- **AND** no broken image tag appears

#### Scenario: Real photo renders when photoUrl is set

- **WHEN** a marina card has a non-null `photoUrl`
- **THEN** the image slot renders an `<img src={photoUrl}>` with `object-cover` styling

---

### Requirement: Result summary line on marina rollup search

The marina rollup result list SHALL display a summary line above the cards showing the count of marinas found in the current viewport: "N marina(s) in view". This line SHALL update each time search results change.

#### Scenario: Summary reflects result count

- **WHEN** a search returns 4 marina rows
- **THEN** the summary reads "4 marinas in view"

#### Scenario: Singular form for one marina

- **WHEN** a search returns exactly 1 marina row
- **THEN** the summary reads "1 marina in view"

#### Scenario: Empty results show zero state

- **WHEN** a search returns 0 marina rows
- **THEN** the summary reads "0 marinas in view" or an appropriate empty-state message is shown

---

### Requirement: Amenity filter chips on slip-at-marina search (step 2)

The slip-level search page (`/search/marinas/:marinaId`) SHALL display filter chips for amenities available at the marina. The chips SHALL use the existing `hasElectric` and `hasWater` filter params already supported by `GET /marinas/{id}/slips/search`, plus any additional boolean filters supported by that endpoint.

Chips SHALL display the count of matching slips when a chip is active, e.g. "Electric (3)".

#### Scenario: Electric chip filters slip list

- **WHEN** the user activates the "Electric" chip on the slip-at-marina page
- **THEN** the request to `GET /marinas/{id}/slips/search` includes `hasElectric=true`
- **AND** only slips with electric service appear in the list

#### Scenario: Chip count reflects filtered results

- **WHEN** 3 of 6 slips at the marina have electric service
- **AND** the Electric chip is active
- **THEN** the chip label shows "Electric (3)"
