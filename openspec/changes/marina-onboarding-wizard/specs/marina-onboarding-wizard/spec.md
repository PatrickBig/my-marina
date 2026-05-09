## ADDED Requirements

### Requirement: Marina draft state
A `Marina` SHALL have an `IsSetupComplete` (bool, default `false`) and `SetupStep` (int, default `0`) field. A marina with `IsSetupComplete = false` is considered a draft. Draft marinas SHALL be excluded from all marketplace search results, public profile endpoints, and any external-facing queries via an EF Core global query filter. Draft marinas SHALL remain visible only to users holding a `Membership` at that marina.

#### Scenario: Draft marina excluded from search
- **WHEN** a user searches for available slips
- **THEN** slips belonging to draft marinas (`IsSetupComplete = false`) SHALL NOT appear in results

#### Scenario: Draft marina visible to owner
- **WHEN** a marina owner loads their home page or marina dashboard
- **THEN** their draft marina SHALL appear with a "Draft" indicator

#### Scenario: Draft marina deletion
- **WHEN** a marina owner deletes a draft marina (`IsSetupComplete = false`)
- **THEN** the system SHALL cascade-delete all associated docks, slips, and memberships and return 204
- **WHEN** a marina owner attempts to delete a non-draft marina (`IsSetupComplete = true`)
- **THEN** the system SHALL return 409 (deletion of active marinas requires a future admin flow)

### Requirement: Home page entry points for onboarding
The home page SHALL show a dismissible setup banner when the authenticated user has no marinas and no active onboarding drafts. The "My Marinas" section SHALL render a distinct "draft" card variant for each draft marina, showing the marina name, a "Continue setup →" link to `/marina/{id}/setup`, and a "Delete draft" action.

#### Scenario: Banner shown to new user with no marinas
- **WHEN** an authenticated user with zero marinas (no staff memberships) loads the home page
- **THEN** a dismissible "Set up your marina to get started" banner SHALL appear above the "My Marinas" section

#### Scenario: Banner dismissed
- **WHEN** the user clicks "Dismiss" on the setup banner
- **THEN** the banner SHALL not reappear for that browser session (stored in localStorage)

#### Scenario: Draft marina card shown
- **WHEN** a user has at least one draft marina
- **THEN** each draft marina SHALL render as a card with a "Draft" badge, "Continue setup →" and "Delete draft" actions instead of the normal "Open dashboard →" link

### Requirement: Wizard entry and routing
The `/marina/new` route SHALL redirect to step 1 of the wizard. Step 1 SHALL create the Marina in draft state (POST to backend) and then redirect to `/marina/{id}/setup`. The wizard SHALL be accessible at `/marina/{id}/setup` and remember the user's current step via `Marina.SetupStep`. If a user navigates directly to `/marina/{id}/setup` for a draft marina they own, the wizard SHALL resume at the last completed step.

#### Scenario: Starting wizard from home page
- **WHEN** a user clicks "+ Add marina" or "Set up a marina" from the home page
- **THEN** the browser SHALL navigate to `/marina/new` which renders step 1 of the wizard

#### Scenario: Marina created on step 1 submit
- **WHEN** the user submits the marina profile form (step 1)
- **THEN** the system SHALL create the Marina with `IsSetupComplete = false`, `SetupStep = 1`, and redirect to `/marina/{id}/setup`

#### Scenario: Wizard resumes at correct step
- **WHEN** a user navigates to `/marina/{id}/setup` for a draft marina they own
- **THEN** the wizard SHALL load at the step indicated by `Marina.SetupStep`, pre-populating any already-saved data

### Requirement: GPS location step with geocoder
The marina location step SHALL provide an explicit "Locate on map" button that triggers a Nominatim geocoding request. The system SHALL attempt geocoding with a progressive fallback chain: (1) full address, (2) city + state + zip, (3) city + state, (4) state only. On a successful match, the system SHALL notify the user of the precision level found (e.g., "Showing Sarasota, FL — drag the pin to your exact marina location"). On all fallback levels, a Leaflet map with a draggable pin SHALL be displayed. The pin's position SHALL update the latitude/longitude fields in real time. If no geocoding result is found, the map SHALL remain visible for manual pin placement with an informational message.

#### Scenario: Full address geocoded successfully
- **WHEN** the user enters a full address and clicks "Locate on map"
- **THEN** the system SHALL request coordinates from Nominatim using the full address
- **THEN** the map SHALL fly to the result and place a draggable pin at the coordinates

#### Scenario: Partial address fallback
- **WHEN** the full address geocoding attempt returns no results
- **THEN** the system SHALL retry with city + state + zip, then city + state, then state only
- **THEN** on any successful fallback result, the system SHALL display a message indicating the precision level (e.g., "Showing Florida — drag the pin to your exact location")

#### Scenario: No geocoding result
- **WHEN** all fallback attempts return no results
- **THEN** the system SHALL display "We couldn't locate that address. Use the map to place your pin manually."
- **THEN** the Leaflet map SHALL remain visible centered at the country level for manual placement

#### Scenario: Draggable pin updates coordinates
- **WHEN** the user drags the Leaflet pin to a new position
- **THEN** the latitude and longitude fields SHALL update immediately to reflect the pin's new coordinates

### Requirement: Wizard localStorage crash recovery
The wizard SHALL persist all in-progress state to localStorage keyed by `marina-setup-{marinaId}`. State SHALL be written to localStorage synchronously on every change. Backend sync SHALL occur on step transitions and when the user clicks an explicit "Save progress" button. On wizard load, the system SHALL compare the localStorage timestamp against the marina's `UpdatedAt` timestamp from the backend; whichever is newer SHALL be used as the working state.

#### Scenario: Page reload recovery
- **WHEN** the user refreshes the browser mid-wizard
- **THEN** all wizard state SHALL be restored from localStorage without data loss

#### Scenario: Recovery on different device
- **WHEN** localStorage is empty (new device or cleared storage)
- **THEN** the wizard SHALL load state from the backend (last step-synced state)

#### Scenario: Step transition triggers backend sync
- **WHEN** the user advances from one wizard step to the next
- **THEN** the current step's data SHALL be persisted to the backend before navigation proceeds

### Requirement: Publish step and marina activation
The final wizard step SHALL display a summary of the marina configuration (name, type, dock count, slip count). The step SHALL include an "List on the marketplace" toggle defaulting to OFF. Educational copy SHALL explain that enabling the toggle means boaters can immediately attempt to book slips. On submit, the system SHALL set `Marina.IsSetupComplete = true` and `Marina.IsListed` per the toggle value.

#### Scenario: Default publish state
- **WHEN** the user reaches the publish step
- **THEN** the "List on marketplace" toggle SHALL be OFF by default

#### Scenario: Marina activated without listing
- **WHEN** the user submits the publish step with the toggle OFF
- **THEN** `Marina.IsSetupComplete` SHALL be set to `true` and `Marina.IsListed` SHALL be `false`
- **THEN** the user SHALL be redirected to `/marina/{id}` (the marina dashboard)

#### Scenario: Marina activated and listed
- **WHEN** the user submits the publish step with the toggle ON
- **THEN** `Marina.IsSetupComplete` SHALL be set to `true` and `Marina.IsListed` SHALL be `true`
- **THEN** the marina's slips SHALL become eligible to appear in marketplace search results
