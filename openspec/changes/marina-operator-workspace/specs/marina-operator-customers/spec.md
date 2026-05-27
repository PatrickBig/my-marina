## ADDED Requirements

### Requirement: Customers screen with URL filters and pagination
`/marina/:marinaId/accounts` SHALL render a paginated table of `BillingAccount` entities labelled "Customers" in the UI. URL params: `status` (default: `all`; values: `all | active | overdue | dockominium | invited`), `id` (selected account, drawer open), `page` (default: 1), `q` (free-text search). Page size: 25 rows. Visual spec: `docs/design_handoff_mymarina_marina_operator/screens-customers-money.md#customers`.

#### Scenario: Search query filters by name, email, or vessel
- **WHEN** an operator types "smith" in the search field
- **THEN** only accounts matching "smith" in name, email, or vessel name are shown, and `?q=smith` is in the URL

#### Scenario: Overdue chip filters to accounts with outstanding balance
- **WHEN** an operator clicks the Overdue chip
- **THEN** only accounts with at least one overdue invoice are shown

### Requirement: Customer detail drawer bound to ?id
Clicking a table row SHALL set `?id=<accountId>` and open a detail drawer showing: members, vessels, open invoices (click → billing screen), and action buttons (Invoice/Payment/Message). Closing clears `?id`. At ≥ 1100 px: right-side column; below: `<Sheet>`. Visual spec: `docs/design_handoff_mymarina_marina_operator/screens-customers-money.md#drawer`.

#### Scenario: Reload with ?id opens the drawer
- **WHEN** a user loads the URL with `?id=<accountId>`
- **THEN** the detail drawer is open for that account

#### Scenario: Invoice link in drawer navigates to billing with id
- **WHEN** an operator clicks an invoice in the customer drawer
- **THEN** navigation goes to `/marina/:id/billing?id=<invoiceId>`
