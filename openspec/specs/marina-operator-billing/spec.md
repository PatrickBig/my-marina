## ADDED Requirements

### Requirement: Billing screen KPI tiles
`/marina/:marinaId/billing` SHALL render four KPI tiles at the top of the screen using data from `getBillingSummary(marinaId)`: Outstanding, Overdue, MTD Collected, and Aging Buckets. The Aging Buckets tile SHALL render an inline bar chart (no charting library — inline CSS/divs). KPI tile values SHALL not require downloading the full invoice list. Visual spec: `docs/design_handoff_mymarina_marina_operator/screens-customers-money.md#billing`.

#### Scenario: KPI tiles show correct outstanding amount
- **WHEN** the billing summary has $2,400 outstanding across 4 invoices
- **THEN** the Outstanding tile shows "$2,400" and "4 invoices"

#### Scenario: Overdue amount is coloured destructive
- **WHEN** there is any overdue amount
- **THEN** the Overdue tile's value text uses the destructive colour token

### Requirement: Invoice table with status filter and pagination
The billing screen SHALL render a paginated table of invoices. URL params: `status` (default: `all`; values: `all | open | overdue | partial | paid | voided`), `id` (selected invoice, drawer open), `page` (default: 1), `q` (search). Page size: 20 rows. Voided rows SHALL render at ~55% opacity. Context-sensitive action buttons: Overdue → Remind; Open/Partial → Record payment; Paid/Voided → View. Visual spec: `docs/design_handoff_mymarina_marina_operator/screens-customers-money.md#table`.

#### Scenario: Status filter chips update the URL and list
- **WHEN** an operator clicks the Overdue chip
- **THEN** `?status=overdue` is in the URL and only overdue invoices are shown

#### Scenario: Voided invoices render at reduced opacity
- **WHEN** a voided invoice appears in the list
- **THEN** the row has approximately 55% opacity

### Requirement: Invoice detail drawer
Clicking an invoice row SHALL set `?id=<invoiceId>` and open a right-side detail drawer showing: invoice number, account, status badge, line items, payment history, and context-sensitive actions (Mark paid / Apply partial / Void / Send PDF — PDF labeled "post-MVP"). Closing clears `?id`.

#### Scenario: Reload with ?id opens invoice drawer
- **WHEN** a user loads `/marina/:id/billing?id=<invoiceId>`
- **THEN** the detail drawer is open for that invoice

### Requirement: Billing mutations invalidate counters and list
`recordPayment`, `voidInvoice`, and `sendInvoice` mutations SHALL invalidate `['marina-invoices', marinaId]` and `['marina-counters', marinaId]` on success.

#### Scenario: Recording a payment updates overdue badge
- **WHEN** an operator records full payment on the last overdue invoice
- **THEN** the Billing sidebar badge (overdue count) drops to zero
