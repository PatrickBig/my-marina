## ADDED Requirements

### Requirement: Slips screen with dock filter rail and pagination
`/marina/:marinaId/slips` SHALL render a dock filter rail and a paginated slip table. URL params: `dock` (selected dock id, default: first dock), `status` (default: `active`; values: `active | maint | inactive | listed | all`), `plan` (pricing plan id filter, for deep-links from Pricing), `page` (default: 1). Page size: 10 rows. At ≥ 900 px the rail is a left sidebar (220 px); below that a grid above the table. Visual spec: `docs/design_handoff_mymarina_marina_operator/screens-marina-setup.md#slips--docks`.

#### Scenario: Dock rail selection filters the slip table
- **WHEN** an operator clicks a dock in the rail
- **THEN** `?dock=<dockId>` is in the URL and only slips in that dock are shown

#### Scenario: ?plan filter shows a dismissible banner
- **WHEN** the URL includes `?plan=<planId>`
- **THEN** a banner reads "Filtered by plan: [plan name] · N slips · Clear" and only slips assigned that plan are shown

#### Scenario: Clearing the plan filter removes the banner
- **WHEN** an operator clicks Clear on the plan filter banner
- **THEN** `?plan` is removed from the URL

### Requirement: Add/edit slip and dock via dialogs
`PageHeader` SHALL have "+ Dock" (secondary) and "+ Slip" (primary) buttons. Both open the existing forms in a Radix `<Dialog>`. Delete actions use `<AlertDialog>` for confirmation.

#### Scenario: Deleting a slip requires confirmation
- **WHEN** an operator clicks Delete on a slip
- **THEN** an AlertDialog appears asking for confirmation before the delete API call is made
