## ADDED Requirements

### Requirement: Dashboard is the default landing screen
The route `/marina/:marinaId/dashboard` SHALL be the default landing screen when a marina operator enters the workspace. Visual layout spec: `docs/design_handoff_mymarina_marina_operator/screens-operations.md#dashboard`.

#### Scenario: Default redirect lands on dashboard
- **WHEN** a user navigates to `/marina/:id`
- **THEN** they are redirected to `/marina/:id/dashboard`

### Requirement: Occupancy ring and composition bar
The dashboard SHALL render an occupancy ring (SVG donut, filled/total slips ratio) and a composition bar (horizontal stacked bar segmented by assignment type) using data from `getMarinaComposition(marinaId)`. No charting library SHALL be introduced; both components are inline SVG/CSS. Visual spec: `docs/design_handoff_mymarina_marina_operator/screens-operations.md#occupancy-ring-component`.

#### Scenario: Occupancy ring shows correct percentage
- **WHEN** the marina has 40 filled slips out of 80 total
- **THEN** the ring displays "50%" and "40 / 80 slips"

#### Scenario: Composition bar segments reflect assignment types
- **WHEN** the marina has 20 annual, 10 seasonal, 5 transient, and 5 vacant slips
- **THEN** the bar shows four proportional segments

### Requirement: KPI tiles deep-link to filtered screens
The dashboard SHALL render 4 KPI tiles using `useMarinaCounters(marinaId)` and `getBillingSummary(marinaId)`. Each tile SHALL be clickable and navigate to the corresponding filtered screen. Tile component: `src/MyMarina.Web/src/components/ui/kpi.tsx`.

| Tile | Destination | Search params |
|---|---|---|
| Pending requests | `/reservations` | `{ status: 'pending' }` |
| Open invoices | `/billing` | `{ status: 'open' }` |
| MTD earnings | `/billing` | `{ status: 'paid' }` |
| Open work orders | `/maintenance` | `{ col: 'inprogress' }` |

#### Scenario: Clicking a KPI tile navigates with correct filter
- **WHEN** an operator clicks the "Open work orders" tile
- **THEN** the browser navigates to `/marina/:id/maintenance?col=inprogress`

### Requirement: Tabbed inbox with deep-link rows
The dashboard SHALL render a tabbed inbox card with tabs for Reservations, Work orders, Billing, and Sublets. Tab state is local component state (not URL-bound — it is a "peek" not a deep-link surface). Each row in each tab SHALL navigate to the corresponding screen with the appropriate `?id` and `?status` params on click.

#### Scenario: Inbox reservation row navigates with id
- **WHEN** an operator clicks a reservation row in the inbox
- **THEN** navigation goes to `/marina/:id/reservations?id=<reservationId>&status=pending`
