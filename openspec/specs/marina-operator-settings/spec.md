## ADDED Requirements

### Requirement: Settings screen with sub-tabs
`/marina/:marinaId/settings` SHALL render a sub-tabbed settings screen. The active tab SHALL be tracked in `?tab` (default: `profile`; values: `profile | address | hours | photos | subscription`). Sub-tabs SHALL be rendered as a `<Tabs>` strip below `PageHeader`. Each tab's content renders in a single `<Card>` with a two-column label/input grid (single column below 900 px). Visual spec: `docs/design_handoff_mymarina_marina_operator/screens-marina-setup.md#settings`.

#### Scenario: Default tab is Profile
- **WHEN** an operator navigates to `/marina/:id/settings`
- **THEN** the Profile tab is active

#### Scenario: Tab navigation updates the URL
- **WHEN** an operator clicks the Hours tab
- **THEN** the URL becomes `?tab=hours`

### Requirement: Profile and Address tabs use existing MarinaInfoPanel form
The Profile and Address tabs SHALL be populated by extracting and splitting the existing `MarinaInfoPanel` from `MarinaDashboardPage.tsx`. The same `react-hook-form` + Zod schema SHALL be used. Profile covers: name, type, contact, website, description. Address covers: street, city/state/zip, coordinates (with auto-geocode button), map preview, timezone.

#### Scenario: Auto-geocode button fills coordinates from address
- **WHEN** an operator enters an address and clicks "Auto-fill from address"
- **THEN** the latitude and longitude fields are populated via the Nominatim geocoding call

### Requirement: Hours & policy tab
The Hours tab SHALL render inputs for summer hours, off-season hours, approval policy (Instant book / Request to book), and auto-decline timeout. These fields SHALL be part of the `updateMarina` form payload.

#### Scenario: Hours tab saves correctly
- **WHEN** an operator updates summer hours and clicks Save
- **THEN** `updateMarina` is called with the new hours data

### Requirement: Photos tab surfaces existing upload flow
The Photos tab SHALL render the photo grid using the existing `PhotoCard` components and `usePhotoUpload` hook. The "+ Upload" button SHALL open `CropUploadModal`. The first photo SHALL display a "Cover" badge. Photos SHALL be reorderable by drag (using the existing drag handle from `PhotoCard`).

#### Scenario: Uploading a photo appears in the grid
- **WHEN** an operator uploads a photo via CropUploadModal
- **THEN** the new photo appears in the grid after the upload completes

### Requirement: Subscription tab is read-only
The Subscription tab SHALL display the marina's current plan (Free/Pro/Premium), renewal date, price, and a feature matrix. The "Change plan" button SHALL be rendered but labeled as disabled/post-MVP. No subscription change functionality is implemented in v1.

#### Scenario: Current plan is displayed
- **WHEN** a marina is on the Pro plan
- **THEN** the Subscription tab shows "Pro" with the correct renewal date and price

#### Scenario: Change plan button is disabled
- **WHEN** an operator views the Subscription tab
- **THEN** the "Change plan" button is present but disabled with a "Post-MVP" label or tooltip

### Requirement: Save applies to the active tab only
The page-level "Save changes" button in `PageHeader` SHALL be disabled until the current tab's form is dirty. Clicking it saves only the currently active tab's data via `updateMarina`.

#### Scenario: Save button is disabled on clean form
- **WHEN** an operator has not changed any fields on the active tab
- **THEN** the Save button is disabled
