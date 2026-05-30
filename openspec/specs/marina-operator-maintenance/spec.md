## ADDED Requirements

### Requirement: Maintenance screen with board/list toggle
`/marina/:marinaId/maintenance` SHALL render a kanban board (default) or list view toggled by `?view` (`board | list`, default: `board`). The view toggle SHALL persist to the URL. Visual spec: `docs/design_handoff_mymarina_marina_operator/screens-operations.md#maintenance`.

#### Scenario: Default view is the kanban board
- **WHEN** a user navigates to `/marina/:id/maintenance`
- **THEN** the board view with four columns (New, Scheduled, In progress, Completed) is shown

#### Scenario: List toggle updates URL and switches view
- **WHEN** an operator clicks the List toggle
- **THEN** the URL gains `?view=list` and a single table replaces the columns

### Requirement: Completed column has a time-range filter
The Completed column (board) and completed rows (list) SHALL be filtered by `?done` (`7d | 30d | all`, default: `7d`). The filter control SHALL appear in the Completed column header (board) and above the table (list). When fewer items are shown than exist, a `N / total` indicator SHALL be visible.

#### Scenario: Default hides completions older than 7 days
- **WHEN** there are 5 completions in the last 7 days and 20 total
- **THEN** only 5 cards are shown and "5 / 20" is displayed

### Requirement: Dashboard col deep-link
When `?col` is present (values: `new | scheduled | inprogress | done`), a dismissible banner SHALL appear reading "Filtered to [column] via dashboard link" and only that column/status SHALL be shown. The banner's Clear button removes `?col` from the URL.

#### Scenario: Arriving from dashboard with ?col=inprogress
- **WHEN** the URL is `/marina/:id/maintenance?col=inprogress`
- **THEN** only In-progress items are visible and the filter banner is shown

#### Scenario: Clearing the col filter shows all columns
- **WHEN** the operator clicks Clear on the col filter banner
- **THEN** `?col` is removed and all columns are shown

### Requirement: Status-change mutations invalidate counters
Mutations that change a work order's status (`updateWorkOrder`, `updateMaintenanceRequestStatus`) SHALL invalidate `['marina-counters', marinaId]` in their `onSuccess` callbacks.

#### Scenario: Closing a work order updates sidebar badge
- **WHEN** an operator moves a work order to Completed
- **THEN** the Open work orders sidebar badge count decreases
