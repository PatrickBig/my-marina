## ADDED Requirements

### Requirement: Listings slip picker
`/marina/:marinaId/listings` (no slipId) SHALL render a table of listable slips showing: slip number, dimensions, listing status (Listed/Paused/Not listed), open window count, and MTD earnings. Clicking a row navigates to `/listings/:slipId`. Visual spec: `docs/design_handoff_mymarina_marina_operator/screens-operations.md#listings`.

#### Scenario: Slip picker shows all listable slips
- **WHEN** an operator navigates to `/marina/:id/listings`
- **THEN** all slips belonging to the marina are listed

#### Scenario: Clicking a slip opens its calendar
- **WHEN** an operator clicks a slip row
- **THEN** navigation goes to `/marina/:id/listings/:slipId`

### Requirement: Availability calendar with react-day-picker
`/marina/:marinaId/listings/:slipId` SHALL render a month-grid calendar using `react-day-picker` and a window editor panel. The calendar SHALL visually distinguish: empty cells (no window), open windows (primary tint + price overlay), paused windows (muted/dashed), and booked cells (solid fill). The currently selected availability window id SHALL be tracked in `?windowId`. Visual spec: `docs/design_handoff_mymarina_marina_operator/screens-operations.md#layout--calendar-editor-with-slipid`.

#### Scenario: Calendar loads existing windows
- **WHEN** an operator opens a slip's calendar
- **THEN** all existing availability windows for that slip are visually represented on the calendar

#### Scenario: Selecting a window opens the editor
- **WHEN** an operator clicks a cell that belongs to an availability window
- **THEN** the window editor panel shows that window's details and `?windowId` is set in the URL

### Requirement: Pointer-event drag creates new availability windows
The calendar SHALL support click-and-drag to select a date range and create a new `AvailabilityWindow`. A `useDateRangeDrag` hook SHALL track `dragStart` / `dragEnd` state via `onPointerDown`, `onPointerMove`, and `onPointerUp` events. Dragging over dates already covered by an existing window SHALL be prevented and show a toast error.

#### Scenario: Dragging a range opens window creation
- **WHEN** an operator drags from day 5 to day 10 on an empty month
- **THEN** days 5–10 are highlighted and a "Create window" confirmation appears

#### Scenario: Overlap with existing window is blocked
- **WHEN** an operator attempts to drag over dates already in an active availability window
- **THEN** the drag is prevented and a toast error reads "Date range overlaps an existing window"

### Requirement: Window editor saves via page-level action
The window editor panel SHALL render pricing inputs, booking policy toggles, and status controls. The page-level "Save" button (in `PageHeader`) SHALL call `updateAvailabilityWindow` and invalidate `['windows', marinaId, slipId]`. Pause window and Delete window are separate secondary actions.

#### Scenario: Saving a window persists changes
- **WHEN** an operator edits a window's base price and clicks Save
- **THEN** `updateAvailabilityWindow` is called and the calendar reflects the new price
