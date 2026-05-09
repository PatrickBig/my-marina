## ADDED Requirements

### Requirement: Extensible dock naming conventions
The dock builder SHALL support a typed set of naming conventions, each implemented as a pure generator function `generateDockName(convention, index, config) → string`. Adding a new convention SHALL require only a new generator function and a corresponding UI case — no changes to callers. The system SHALL support at minimum: `Lettered` (A, B, C… with optional prefix/suffix), `Numbered` (1, 2, 3… with optional prefix/suffix), and `Manual` (user enters each name individually). Convention configuration parameters (prefix, suffix, startAt) SHALL be persisted as part of the wizard draft state.

#### Scenario: Lettered dock names with prefix
- **WHEN** the user selects "Lettered" with prefix "Dock " and creates 3 docks
- **THEN** dock names SHALL be generated as "Dock A", "Dock B", "Dock C"

#### Scenario: Numbered dock names
- **WHEN** the user selects "Numbered" with no prefix and creates 5 docks
- **THEN** dock names SHALL be generated as "1", "2", "3", "4", "5"

#### Scenario: Manual dock names
- **WHEN** the user selects "Manual"
- **THEN** a text input SHALL appear for each dock allowing free-form name entry

### Requirement: Extensible slip naming conventions
The slip builder SHALL support a typed set of naming conventions, each implemented as a pure generator function `generateSlipName(convention, dockIndex, slipIndex, totalSlipsBefore, config) → string`. The system SHALL support at minimum: `PerDockReset` (counter resets per dock: A-1…A-10, B-1…B-10), `PerDockGlobal` (counter continues across docks: A-1…A-10, B-11…B-20), `Sequential` (no dock prefix, pure numbers across all docks: 1…300), and `Manual` (user enters each slip name). Convention configuration parameters (separator, prefix, suffix, startAt, padZeros) SHALL be persisted as part of the wizard draft state.

#### Scenario: Per-dock reset naming
- **WHEN** the user selects "PerDockReset" with separator "-" and 2 docks of 10 slips each (dock names A and B)
- **THEN** slip names SHALL be "A-1" through "A-10", then "B-1" through "B-10"

#### Scenario: Per-dock global naming
- **WHEN** the user selects "PerDockGlobal" with separator "-" and 2 docks of 10 slips each (dock names A and B)
- **THEN** slip names SHALL be "A-1" through "A-10", then "B-11" through "B-20"

#### Scenario: Sequential naming
- **WHEN** the user selects "Sequential" with startAt=1 and 2 docks of 10 slips each
- **THEN** slip names SHALL be "1" through "20" with no dock prefix

### Requirement: Dock-level dimension and amenity defaults
The dock builder SHALL allow the user to set default slip properties per dock: `MaxLength`, `MaxBeam`, `MaxDraft`, `SlipType`, `HasElectric`, `Electric`, `HasWater`, `HasPumpOut`, `IsCovered`, `IsIndoor`, and `Amenities` (custom tags). These defaults SHALL pre-populate all slips belonging to that dock when the draft structure is generated. Defaults SHALL be overridable at the individual slip level in the preview/adjust step. The UI SHALL indicate which slips have been individually overridden.

#### Scenario: Dock defaults applied to all slips
- **WHEN** the user sets MaxLength=35, HasElectric=true, Electric=Amp30 as defaults for Dock A
- **THEN** all generated slips in Dock A SHALL have MaxLength=35, HasElectric=true, Electric=Amp30

#### Scenario: Individual slip override
- **WHEN** the user edits a single slip's MaxLength in the preview table to 60
- **THEN** that slip's MaxLength SHALL be 60 while other slips in the dock retain the default

### Requirement: Uniform or per-dock slip count
The dock builder SHALL allow the user to specify either a single uniform slip count applied to all docks, or a different slip count per dock. The UI SHALL default to uniform count with an option to switch to per-dock entry.

#### Scenario: Uniform slip count
- **WHEN** the user selects "Same for all docks" and enters 20
- **THEN** all docks SHALL be generated with 20 slips each

#### Scenario: Per-dock slip count
- **WHEN** the user selects "Different per dock"
- **THEN** an input SHALL appear for each dock allowing individual slip counts

### Requirement: Preview and adjust table
The system SHALL render a preview table of the entire draft dock+slip structure, grouped and collapsible by dock. The table SHALL support: bulk edit for all slips in a dock (change a field for every slip in the dock at once), inline edit for individual slip fields, adding a slip to a dock, removing a slip, and removing an entire dock. Changes in the preview table SHALL update localStorage immediately and sync to the backend on debounce or explicit save.

#### Scenario: Collapsible dock groups
- **WHEN** the user clicks a dock group header
- **THEN** the slips for that dock SHALL collapse or expand

#### Scenario: Bulk edit a dock
- **WHEN** the user invokes "Edit all slips in Dock A" and changes MaxLength to 40
- **THEN** all slips in Dock A SHALL have MaxLength updated to 40 in both local state and backend

#### Scenario: Inline slip edit
- **WHEN** the user clicks a cell in the preview table
- **THEN** the cell SHALL become editable inline and commit on blur or Enter

#### Scenario: Add slip to dock
- **WHEN** the user clicks "+ Add slip" within a dock group
- **THEN** a new slip SHALL be appended with the dock's default values and a generated name following the current convention

#### Scenario: Remove a slip
- **WHEN** the user clicks the delete icon on a slip row
- **THEN** the slip SHALL be removed from the table and the backend draft

#### Scenario: Remove a dock
- **WHEN** the user clicks "Remove dock" on a dock group header
- **THEN** the dock and all its slips SHALL be removed from the table and the backend draft

### Requirement: Batch dock/slip endpoint
The system SHALL provide a `PUT /marinas/{id}/setup/docks` endpoint that atomically replaces the entire draft dock and slip tree for a marina. The endpoint SHALL accept a payload of the form `{ docks: [{ name, slips: [{ name, maxLength, maxBeam, maxDraft, slipType, hasElectric, electric, hasWater, hasPumpOut, isCovered, isIndoor, amenities }] }] }`. The endpoint SHALL only be callable on draft marinas (`IsSetupComplete = false`). The payload shape SHALL be compatible with the future spreadsheet import feature (same contract, different input surface). The operation SHALL be transactional — all docks and slips are replaced atomically or the operation fails entirely.

#### Scenario: Successful bulk replace
- **WHEN** the wizard calls `PUT /marinas/{id}/setup/docks` with a valid payload
- **THEN** all existing draft docks and slips for that marina SHALL be deleted and replaced with the payload contents atomically

#### Scenario: Rejected on active marina
- **WHEN** `PUT /marinas/{id}/setup/docks` is called on a marina where `IsSetupComplete = true`
- **THEN** the system SHALL return 409 Conflict

#### Scenario: Partial failure rolls back
- **WHEN** an error occurs mid-transaction during dock/slip replacement
- **THEN** no partial state SHALL be persisted; the existing dock/slip data SHALL remain unchanged
