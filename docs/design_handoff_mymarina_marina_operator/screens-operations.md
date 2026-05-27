# Screens · Operations

Dashboard · Reservations · Maintenance · Listings.

Each section below maps to one route under `/marina/$marinaId/`. For URL params,
defaults, and validation rules, also see [`routing.md`](./routing.md).

---

## Dashboard

`/marina/:marinaId/dashboard`

### Purpose

The landing page after a marina is selected. One screen, glanceable, with click-
through navigation to the relevant filtered surface.

### Layout

Hero row at top (1.6fr / 1fr at desktop, single-column below 1100 px):

1. **Occupancy card** (left, larger):
   - Big SVG donut showing held/total slip ratio. Single `<circle>` for the
     track, single `<circle>` for the filled arc using `stroke-dasharray`.
   - Composition section to the right: a horizontal stacked bar segmented by
     assignment type (Annual · Seasonal · Monthly · Transient · Listed · Vacant
     · Maintenance) with `flex: <count>` on each segment.
   - 2-column legend grid below, each row = swatch + label + count + hint.
   - A "By dock →" link in the corner navigates to `/slips`.
2. **KPI stack** (right, 4 tiles in a 2×2 grid → 4-up at narrower → 2×2 again
   at mobile):
   - Pending requests · 4 (Oldest 18h) — accent tinted icon
   - Open invoices · $8,420 (11 accounts)
   - MTD earnings · $3,180 (+18% delta badge)
   - Open work orders · 7 (2 on-hold)

Each KPI tile is **clickable**. The hover state lifts the border and adds a
"View →" affordance. Implementation:

```tsx
<KPI label="Open work orders" value={counters.openWorkOrders} hint="…"
  onClick={() => navigate({
    to: '/marina/$marinaId/maintenance',
    params: { marinaId },
    search: { col: 'inprogress' },
  })}
/>
```

Mapping:

| Tile | Destination | Search params |
|---|---|---|
| Pending requests | `reservations` | `{ status: 'pending' }` |
| Open invoices | `billing` | `{ status: 'open' }` |
| MTD earnings | `billing` | `{ status: 'paid' }` |
| Open work orders | `maintenance` | `{ col: 'inprogress' }` |

### Inbox card

Single card under the hero. Header has a 4-tab segmented control:

- Reservations · 4 — list of pending requests, each row clickable → `reservations?id=<id>&status=pending`.
- Work orders · 7 → `maintenance?col=inprogress`.
- Billing · 3 → `billing?status=overdue`.
- Sublets · 2 → `listings`.

Tab state lives in dashboard local state (it's an "I want to peek at" toggle,
not a deep-link surface). Row clicks deep-link.

### Occupancy ring component

```tsx
function OccupancyRing({ filled, total, size = 152 }: Props) {
  const pct = filled / total;
  const r = (size - 16) / 2;
  const c = 2 * Math.PI * r;
  const stroke = 12;
  return (
    <div className="relative" style={{ width: size, height: size }}>
      <svg width={size} height={size} className="-rotate-90">
        <circle cx={size/2} cy={size/2} r={r}
                stroke="var(--muted)" strokeWidth={stroke} fill="none" />
        <circle cx={size/2} cy={size/2} r={r}
                stroke="var(--primary)" strokeWidth={stroke} fill="none"
                strokeDasharray={`${c * pct} ${c}`} strokeLinecap="round" />
      </svg>
      <div className="absolute inset-0 flex flex-col items-center justify-center">
        <div className="text-[28px] font-semibold tabular-nums leading-none">
          {Math.round(pct * 100)}%
        </div>
        <div className="text-xs text-muted-foreground mt-1">
          {filled} / {total} slips
        </div>
      </div>
    </div>
  );
}
```

### Data dependencies

- `useMarinaCounters(marinaId)` for KPI numbers + tab counts.
- A new `getMarinaComposition(marinaId)` server endpoint returning the
  assignment-type breakdown. If you don't want to add an endpoint, derive it
  client-side from `getSlips` + `getSlipAssignments`. Either works.
- `getMarinaReservations(marinaId, { status: 'PendingApproval' })` for the
  inbox preview rows.

---

## Reservations

`/marina/:marinaId/reservations`

### URL params

| Param | Default | Values |
|---|---|---|
| `status` | `pending` | `all / pending / confirmed / today / past / cancelled` |
| `id` | `undefined` | Reservation id (drawer open) |
| `page` | `1` | 1-indexed |

### Layout

Two-column at ≥ 1100 px: card list on the left, detail panel on the right.
Below 1100 px the detail panel becomes a Radix `<Sheet>` triggered by row click.

Filter chip row at the top mirrors the URL `status` param.

### Card shape

Each reservation row is a `<Card>` (not a table row), 16 px padding, 36 px
avatar, name + vessel + dims line, slip + dates + total line, optional italic
"note" block, status badges, and — for Pending / Host marina items — inline
**Approve / Decline / Message** buttons.

Selected card gets the `selected` state described in `design-system.md`.

### Status badges

| Status | Badge variant | Inline actions? |
|---|---|---|
| Pending | warning, dot | Yes (Approve / Decline) |
| Host marina | accent, dot | Yes (Review) |
| Confirmed | success, dot | No |
| Arrived late | destructive, dot | Yes (No-show / Extend) |
| Cancelled | neutral | No |

### Detail panel content

Header: avatar + name + vessel; close icon (clears `?id`).

Sections (separator between each):

1. Badges row (Insurance ✓, 1st visit, etc.)
2. Slip · Dates · Total · Source — key/value list.
3. **Status flow** — visual stepper through Submitted → PendingApproval →
   Confirmed → Completed with the current state highlighted.
4. Sticky Approve / Decline buttons for Pending and Host-marina rows.

### Data

- `getMarinaReservations(marinaId, filters)` — server-side filter by status
  whenever possible.
- `approveReservation`, `declineReservation`, `markNoShow` — invalidate
  `['marina-reservations', marinaId]` and `['marina-counters', marinaId]`.

---

## Maintenance

`/marina/:marinaId/maintenance`

### URL params

| Param | Default | Values |
|---|---|---|
| `view` | `board` | `board / list` |
| `done` | `7d` | `7d / 30d / all` |
| `col` | undefined | `new / scheduled / inprogress / done` (column filter, e.g. from dashboard) |

### Layout — Board view

Four kanban columns side-by-side, each a 10 px-padded muted-grey panel:

- **New** — customer-submitted requests pending triage. Tone: destructive.
- **Scheduled** — work orders with a scheduled date in the future. Tone: accent.
- **In progress** — work orders currently being worked. Tone: primary.
- **Completed** — closed work orders. Tone: success.

Each card is a `<Card>` showing:

- Kind badge (Request / Recurring / Work order) + optional On-hold badge.
- Title.
- "Reporter · when" line.
- Optional ETA pill (in-progress only).
- Optional completion note (green tinted block).
- Footer row: assignee (avatar + name) on left, priority badge on right.

Card header right-side shows a `+` icon button on every column except Completed.
**Completed shows a Last 7 days / Last 30 days / All time select** that drives
the `done` URL param. Defaults to 7 days. **This prevents the completed list from
growing unbounded** — when 30 days is selected, show `N / total` so operators
know how many are hidden.

### Layout — List view

Single table with columns:

| Column | Notes |
|---|---|
| Item | Title |
| Kind | Request / Recurring / Work order |
| Status | New / Scheduled / In progress / Completed |
| Assignee | Avatar + name |
| Priority | Badge |
| Reported | Reporter · date |
| Actions | Open |

Same `done` filter applies — Completed rows beyond the cutoff don't render.

### View toggle

Segmented control in the page header, right side. Persists to `view` URL param.

### `col` filter from dashboard

When `col` is present, a "Filtered to <column> via dashboard link" banner appears
above the columns and only that column renders in board view, or only those rows
render in list view. Banner has a Clear button that clears `col` from the URL.

### Data

- `getMarinaMaintenanceRequests(marinaId)` — Request items (column = New).
- `getMarinaWorkOrders(marinaId)` — Work order items (columns = Scheduled /
  In progress / Completed).
- `updateMaintenanceRequestStatus`, `updateWorkOrder` — mutations that invalidate
  both query keys and the counters.

The Completed `done` filter is applied client-side over the work-orders list —
server doesn't need a date range param unless the dataset is huge.

---

## Listings

`/marina/:marinaId/listings/:slipId?`

### URL params

| Param | Default | Values |
|---|---|---|
| `slipId` (path) | undefined | If unset, show slip picker. If set, show calendar editor for that slip. |
| `windowId` (search) | undefined | Currently selected availability window. |

### Layout — slip picker (no `slipId`)

Table of listable slips with: slip number, dimensions, current listing status
(Listed / Paused / Not listed), number of open windows, MTD earnings. Click row →
navigate to `/listings/<slipId>`.

### Layout — calendar editor (with `slipId`)

Two columns: calendar (left, larger) + window editor (right).

#### Calendar

Month grid. Each cell:

- **No window** — white, light border.
- **Open window** — primary-tinted background with price overlay in lower right.
- **Paused window** — muted background, dashed border.
- **Booked** — solid foreground fill, white text.

Cells are click-and-drag selectable. Drag across a range to create a new
AvailabilityWindow row (overlap with existing windows is prevented — show a
toast). Click an existing window cell to select that window for editing.

Month nav arrows in the calendar header. Legend below the grid.

#### Window editor

Right column shows the selected window. Sections:

1. Pricing inputs: base/night, weekly disc., monthly disc., cleaning fee,
   min/max nights.
2. Booking policy: instant-book toggle, status (Open / Paused / Closed).
3. Revenue split readout (read-only, snapshot value).
4. Action row: Pause window, Delete window.

Save is the page-level primary action in the `PageHeader`.

### Data

- `getAvailabilityWindows(marinaId, { slipId })` — load windows.
- `createAvailabilityWindow`, `setAvailabilityWindowStatus`, update window —
  mutations invalidate `['windows', marinaId, slipId]`.

This screen is also where post-MVP per-day rate overrides would land — leave
room in the layout.
