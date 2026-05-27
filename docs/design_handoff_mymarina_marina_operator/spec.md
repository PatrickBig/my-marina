# Specification — Marina Operator Workspace

## Problem

`src/MyMarina.Web/src/pages/MarinaDashboardPage.tsx` is a single React file:

- **136 KB**, ~3,300 LOC.
- Renders ~12 panels stacked vertically on one route: marina info, docks, slips,
  billing accounts, slip assignments, reservations, availability windows, invoices,
  maintenance, announcements, staff.
- All inline forms, all panel toggles, all expand/collapse state in one component.
- Zero responsive treatment — operators on tablet or phone get the same desktop
  scroll-fest at half the width.

Operators report the screen "doesn't feel very good" — they scroll past most of the
page to find anything, and there's no way to send someone a link to a specific
filter or row.

## Target state

Same data and API surface. New shape:

1. The dashboard route becomes a **workspace shell** that renders nested routes.
   Each panel from today's mega-page becomes its own focused screen.
2. A **left-rail navigation** lists 12 destinations grouped into three sections
   (Operations · Customers & money · Marina setup). The rail collapses to icons at
   tablet width and to a bottom tab bar at mobile width — single-source-of-markup,
   container-queried.
3. **The URL carries filter and selection state.** Every active tab, every selected
   row, every drawer open state appears as a query-string parameter so links are
   shareable and the back button works.
4. **Dashboard widgets navigate** — clicking the "Open work orders" tile opens the
   Maintenance screen pre-filtered to the In-progress column. KPI tiles, inbox rows,
   and composition cells all deep-link.
5. **Pagination replaces "Load all"** on every list (Slips, Customers, Assignments,
   Invoices). Page number is part of the URL state.

## Stack assumptions

Confirmed from the repo:

- React 19, Vite, TypeScript
- Tailwind 4 with `@theme` tokens in `src/MyMarina.Web/src/index.css`
- shadcn primitives in `src/MyMarina.Web/src/components/ui/`
- Radix primitives, Lucide icons, TanStack Query, TanStack Router (installed but
  not yet used for navigation — see `routing.md`)
- `react-hook-form` + Zod for forms (every form in the existing dashboard already
  uses this combo — keep it)

## Decisions (locked)

| Question | Decision |
|---|---|
| One mega-page or many routes? | **Many routes**, nested under the workspace shell. |
| Server-side routing library? | **TanStack Router** (already in `package.json`). See `routing.md` for the file-based vs code-based call. |
| URL state shape? | **Query-string parameters** for filters / selection / pagination. |
| Sidebar form? | **Grouped left rail** at desktop; icon rail at tablet; bottom tab bar at mobile. Container queries on the workspace root. |
| Dashboard variant? | **"Workbench"** — occupancy ring + composition bar + tabbed pivot inbox. The "Today" variant in the prototype is not shipping. |
| Density? | Balanced. Comfortable padding, clear hierarchy. |
| Tables or cards? | Cards for Reservations and Maintenance (visual context matters). Tables for Slips, Customers, Assignments, Invoices, Staff (scannability matters). |
| Slip-map view? | Out of scope for v1. Stays post-MVP. |
| Kanban drag-and-drop? | Out of scope for v1. Status changes are button-driven. List view added for scannability. |

## Constraints (don't change)

- The data model. Routes are 1:1 with the existing panels — they fetch the same
  data via the same calls in `src/MyMarina.Web/src/api/api.ts`.
- The auth/permission junctions (`Membership`, `BillingAccountMember`). They still
  resolve to the same role for the same user. Membership check moves from the
  mega-page to the workspace layout route.
- The existing `NavBar` component. It already has the responsive shape Patrick
  designed and it sits **above** the new workspace shell on every operator route.
- The wizard flows (`MarinaSetupWizardPage`, `MarinaOnboardingPage`,
  `PricingPlansPage`, `MarinaSlipsPage`). They have their own jobs — don't fold
  them into the workspace.

## Scope by feature area

| Area | Goal | Source of truth |
|---|---|---|
| Workspace shell | New | [`shell.md`](./shell.md) |
| Routing + URL state | New pattern + lift-and-shift to TanStack Router | [`routing.md`](./routing.md) |
| Design tokens / primitives | Token-additive (no token churn). New: `Pagination`, `KPI`, semantic Badge variants. | [`design-system.md`](./design-system.md) |
| Dashboard | Replace, new layout | [`screens-operations.md`](./screens-operations.md) |
| Reservations | Lift inbox out of mega-page, add URL-bound tabs | [`screens-operations.md`](./screens-operations.md) |
| Maintenance | Lift out, add Board ↔ List toggle, add Completed-range filter | [`screens-operations.md`](./screens-operations.md) |
| Listings calendar | Lift out, keep editor shape | [`screens-operations.md`](./screens-operations.md) |
| Customers | Lift accounts list out; add URL-bound filter chips + drawer | [`screens-customers-money.md`](./screens-customers-money.md) |
| Assignments | Lift out + pagination + URL filters | [`screens-customers-money.md`](./screens-customers-money.md) |
| Billing (invoices) | **New screen** — KPI tiles + aging + invoice table | [`screens-customers-money.md`](./screens-customers-money.md) |
| Slips & docks | Lift out + dock rail + pagination | [`screens-marina-setup.md`](./screens-marina-setup.md) |
| Pricing plans | **Already exists** at `/marina/:id/pricing` — fold into workspace nav | [`screens-marina-setup.md`](./screens-marina-setup.md) |
| Announcements | Lift out (no behaviour change) | [`screens-marina-setup.md`](./screens-marina-setup.md) |
| Staff | Lift out (no behaviour change) | [`screens-marina-setup.md`](./screens-marina-setup.md) |
| Settings | **New screen** — move `MarinaInfoPanel` + photos + hours + subscription here | [`screens-marina-setup.md`](./screens-marina-setup.md) |

## Acceptance criteria for the whole project

1. `MarinaDashboardPage.tsx` no longer exists. Its content has been split across
   one workspace shell file and 12 route files.
2. Every operator URL hash/path mirrors the visible state — opening the same URL
   in a new tab restores tab, filter, page number, and drawer selection.
3. Every dashboard KPI tile and inbox row navigates to the matching filtered
   screen.
4. At ≥ 1024 px the operator sees the full sidebar; at 720–1023 px an icon rail;
   below 720 px a bottom tab bar. No horizontal scroll at any width.
5. No table on the operator side renders more than `pageSize` rows in the DOM at
   once. "Load all" links are gone.
6. The Maintenance Completed column has a Last-7-days / Last-30-days / All-time
   filter and defaults to Last 7 days.
7. `src/MyMarina.Web/src/components/ui/badge.tsx` exports semantic variants
   (`primary`, `accent`, `success`, `warning`, `destructive`, `neutral`) and they
   look correct in both light and dark mode.
8. `useMarinaCounters(marinaId)` is a single TanStack Query hook that supplies
   the sidebar's pending / overdue / open badge numbers.

## Quality bar

- TypeScript strict. No `any` unless wrapping a third-party gap.
- Each route component is under 500 LOC. If you blow past it, split into
  sub-components in a sibling folder.
- Tests: at minimum, a smoke test per route that mounts it with mocked TanStack
  Query and asserts it renders without throwing.
