## ADDED Requirements

### Requirement: Assignments screen with type filter and pagination
`/marina/:marinaId/assignments` SHALL render a paginated table of active `SlipAssignment` entities. URL params: `type` (default: `all`; values: `all | annual | seasonal | monthly | transient`), `endingSoon` (boolean string; filters to leases ending in next 30 days), `page` (default: 1), `q` (search). Page size: 8 rows. Visual and column spec: `docs/design_handoff_mymarina_marina_operator/screens-customers-money.md#assignments`.

#### Scenario: Ending-soon filter shows leases within 30 days
- **WHEN** `?endingSoon=true` is in the URL
- **THEN** only assignments with an end date within 30 days of today are shown

#### Scenario: Pagination controls render below the table
- **WHEN** there are more than 8 active assignments
- **THEN** the `<Pagination>` component is visible below the table

### Requirement: Add/edit assignment via dialog
The "+ New assignment" button in `PageHeader` SHALL open an existing-form dialog for creating a `SlipAssignment`. Row edit action SHALL open the same dialog pre-populated. The dialog uses the existing `react-hook-form` + Zod form lifted from `MarinaDashboardPage.tsx`.

#### Scenario: Submitting a new assignment closes dialog and refreshes list
- **WHEN** an operator creates a new assignment
- **THEN** `createSlipAssignment` is called, the dialog closes, and the list refreshes
