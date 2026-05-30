## ADDED Requirements

### Requirement: Reservations screen with URL-bound status filter
`/marina/:marinaId/reservations` SHALL render a card list of reservations filtered by `?status` (default: `pending`). Valid values: `all | pending | confirmed | today | past | cancelled`. Visual and card shape spec: `docs/design_handoff_mymarina_marina_operator/screens-operations.md#reservations`.

#### Scenario: Default view shows pending reservations
- **WHEN** a user navigates to `/marina/:id/reservations`
- **THEN** only reservations with status `PendingApproval` are shown

#### Scenario: Status chip updates URL and list
- **WHEN** an operator clicks the "Confirmed" filter chip
- **THEN** the URL becomes `?status=confirmed` and the list updates

### Requirement: Reservations detail drawer bound to ?id
Clicking a reservation card SHALL set `?id=<reservationId>` in the URL and open a detail panel. Closing the panel SHALL clear `?id`. At ≥ 1100 px the panel is a right-side column; below that it is a Radix `<Sheet>`. Visual spec: `docs/design_handoff_mymarina_marina_operator/screens-operations.md#detail-panel-content`.

#### Scenario: Reload with ?id opens the drawer
- **WHEN** a user loads `/marina/:id/reservations?id=abc`
- **THEN** the drawer/sheet opens showing reservation `abc`'s detail

#### Scenario: Closing the drawer clears ?id
- **WHEN** the operator clicks the close button on the detail panel
- **THEN** `?id` is removed from the URL and the panel closes

### Requirement: Inline approve/decline actions
Pending and Host-marina reservation cards SHALL render inline Approve, Decline, and (where applicable) Message buttons. Approve and Decline SHALL call `approveReservation` / `declineReservation` and invalidate `['marina-reservations', marinaId]` and `['marina-counters', marinaId]`.

#### Scenario: Approving a reservation updates the list
- **WHEN** an operator clicks Approve on a pending reservation
- **THEN** the reservation is removed from the pending list and the sidebar counter decreases
