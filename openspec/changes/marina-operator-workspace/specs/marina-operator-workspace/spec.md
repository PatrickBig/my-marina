## ADDED Requirements

### Requirement: MarinaWorkspaceLayout is the parent route for all operator screens
`MarinaWorkspaceLayout` SHALL be a TanStack Router layout route rendered at `/marina/:marinaId`. It SHALL render `<MarinaRail />`, `<main><Outlet /></main>`, and `<MarinaTabBar />`. The `<NavBar />` SHALL be rendered by the `__root__` route, not by this layout. Visual spec: `docs/design_handoff_mymarina_marina_operator/shell.md`.

#### Scenario: Child routes render inside the shell
- **WHEN** a user navigates to `/marina/:id/reservations`
- **THEN** the sidebar and tab bar are visible alongside the Reservations screen content

#### Scenario: Navigating to /marina/:id redirects to dashboard
- **WHEN** a user navigates to `/marina/:id` with no sub-path
- **THEN** the router redirects to `/marina/:id/dashboard`

### Requirement: Workspace auth guard
`MarinaWorkspaceLayout` SHALL check that the authenticated user has a `Marina`-scoped membership with `marinaId` matching the URL param. If the check fails, the user SHALL be redirected to `/`. The check is synchronous (auth state is from Zustand persist); no loading state is required.

#### Scenario: Authorized user sees the workspace
- **WHEN** a user with the correct marina membership navigates to `/marina/:id/dashboard`
- **THEN** the workspace renders normally

#### Scenario: Unauthorized user is redirected
- **WHEN** a user without membership for the marina navigates to `/marina/:id/dashboard`
- **THEN** the user is redirected to `/`

### Requirement: Responsive workspace via container queries
The workspace root element SHALL have `@container/workspace` applied (Tailwind 4 container query). The rail/tab-bar forms SHALL be driven by container width, not viewport width.

| Container width | Form |
|---|---|
| ≥ 1024 px | `<MarinaRail />` — 240 px full sidebar with group labels |
| 720–1023 px | `<MarinaRail />` — 64 px icon-only rail (labels hidden) |
| < 720 px | `<MarinaRail />` hidden; `<MarinaTabBar />` visible |

#### Scenario: Sidebar collapses to icon rail at tablet width
- **WHEN** the workspace container is between 720 and 1023 px wide
- **THEN** nav item labels are hidden and the rail is 64 px wide; icons and counter badges remain visible

#### Scenario: Bottom tab bar appears at mobile width
- **WHEN** the workspace container is below 720 px wide
- **THEN** the rail is hidden and the bottom tab bar shows 5 items

#### Scenario: No horizontal scroll at any width
- **WHEN** the workspace container is 360 px wide
- **THEN** no horizontal scrollbar appears anywhere in the operator workspace

### Requirement: MarinaRail loads marina name from API
The rail header SHALL display the marina's name and tier (e.g., "Pro · Commercial") loaded via `getMarina(marinaId)`. The hardcoded placeholder from the starter code SHALL NOT be used. The query SHALL use `staleTime: Infinity` (marina metadata does not change mid-session).

#### Scenario: Rail header shows real marina name
- **WHEN** the workspace mounts with a valid marinaId
- **THEN** the rail header shows the marina's actual name, not "Big Bay Marina"

### Requirement: Active nav item reflects current pathname
`MarinaRail` and `MarinaTabBar` SHALL highlight the active destination based on `useLocation().pathname`. Search params SHALL NOT affect which item is highlighted.

#### Scenario: Maintenance stays active when column filter is applied
- **WHEN** the URL is `/marina/:id/maintenance?col=inprogress`
- **THEN** the Maintenance nav item is highlighted

### Requirement: useMarinaCounters provides sidebar badge counts
`useMarinaCounters(marinaId)` SHALL be a TanStack Query hook returning `{ pendingReservations, overdueInvoices, openWorkOrders }`. It SHALL use `staleTime: 60_000`. It SHALL be invalidated from the `onSuccess` callback of any mutation that changes one of the underlying counts (approve/decline reservation, record payment, void invoice, update work order status).

#### Scenario: Counter badges show real numbers
- **WHEN** there are 3 pending reservations
- **THEN** the Reservations nav item shows a badge with "3"

#### Scenario: Counter updates after mutation
- **WHEN** an operator approves a reservation
- **THEN** `['marina-counters', marinaId]` is invalidated and the badge count decreases

### Requirement: PageHeader and PageBody scaffold every child route
Every operator screen SHALL use `<PageHeader title="…" />` and `<PageBody>` as its outermost structure. `PageBody` is the only scroll container on the page — no inner card SHALL have its own scroll. Visual spec: `docs/design_handoff_mymarina_marina_operator/shell.md#page-header--body`.

#### Scenario: Screen content does not create nested scroll containers
- **WHEN** a list screen has more items than fit the viewport
- **THEN** only the PageBody scrolls; no inner element has overflow-y: auto or overflow-y: scroll
