# Workspace shell & responsive nav

## What it is

`MarinaWorkspaceLayout` is the parent route component for every operator screen.
It renders:

1. The existing `<NavBar />` at the top (no changes — reused as-is).
2. A **marina-scoped left rail** with 12 destinations grouped into three sections.
3. A `<Outlet />` for the active child route.

The rail collapses based on container width — **never viewport width**. Use a CSS
container query on the workspace root.

## File layout

```
src/MyMarina.Web/src/marina-workspace/
├── MarinaWorkspaceLayout.tsx          ← parent route component
├── MarinaRail.tsx                     ← sidebar / icon rail / bottom tabs
├── MarinaTabBar.tsx                   ← bottom tab bar (mobile)
├── PageHeader.tsx                     ← screen-level header
├── PageBody.tsx                       ← screen-level body wrapper
├── nav-config.ts                      ← MARINA_NAV_GROUPS source of truth
└── useMarinaCounters.ts               ← TanStack Query hook for badge counts
```

## Nav configuration

`nav-config.ts` is the single source of truth — sidebar, mobile tab bar, and the
counters hook all read from it.

```ts
import {
  LayoutGrid, CalendarCheck, Wrench, Calendar,
  Users, ListChecks, Receipt,
  Anchor, DollarSign, Megaphone, Shield, Settings,
} from 'lucide-react';

export type NavId =
  | 'dashboard' | 'reservations' | 'maintenance' | 'listings'
  | 'accounts' | 'assignments' | 'billing'
  | 'slips' | 'pricing' | 'announcements' | 'staff' | 'settings';

export const MARINA_NAV_GROUPS = [
  {
    label: 'Operations',
    items: [
      { id: 'dashboard',    label: 'Dashboard',     icon: LayoutGrid,    counter: null },
      { id: 'reservations', label: 'Reservations',  icon: CalendarCheck, counter: 'pendingReservations' },
      { id: 'maintenance',  label: 'Maintenance',   icon: Wrench,        counter: 'openWorkOrders' },
      { id: 'listings',     label: 'Listings',      icon: Calendar,      counter: null },
    ],
  },
  {
    label: 'Customers & money',
    items: [
      { id: 'accounts',     label: 'Customers',     icon: Users,         counter: null },
      { id: 'assignments',  label: 'Assignments',   icon: ListChecks,    counter: null },
      { id: 'billing',      label: 'Billing',       icon: Receipt,       counter: 'overdueInvoices' },
    ],
  },
  {
    label: 'Marina setup',
    items: [
      { id: 'slips',        label: 'Slips & docks', icon: Anchor,        counter: null },
      { id: 'pricing',      label: 'Pricing plans', icon: DollarSign,    counter: null },
      { id: 'announcements',label: 'Announcements', icon: Megaphone,     counter: null },
      { id: 'staff',        label: 'Staff',         icon: Shield,        counter: null },
      { id: 'settings',     label: 'Settings',      icon: Settings,      counter: null },
    ],
  },
] as const;
```

The mobile tab bar shows 5 items only:

```ts
export const MOBILE_TABS = [
  { id: 'dashboard',    label: 'Home',    icon: LayoutGrid },
  { id: 'reservations', label: 'Res',     icon: CalendarCheck, counter: 'pendingReservations' },
  { id: 'slips',        label: 'Slips',   icon: Anchor },
  { id: 'billing',      label: 'Billing', icon: Receipt,       counter: 'overdueInvoices' },
  { id: 'more',         label: 'More',    icon: Menu },
];
```

The "More" item opens a sheet listing the remaining 8 destinations.

## Counters hook

```ts
// useMarinaCounters.ts
export type MarinaCounters = {
  pendingReservations: number;
  overdueInvoices: number;
  openWorkOrders: number;
};

export function useMarinaCounters(marinaId: string) {
  return useQuery({
    queryKey: ['marina-counters', marinaId],
    queryFn: async (): Promise<MarinaCounters> => {
      const [res, inv, wo] = await Promise.all([
        getMarinaReservations(marinaId, { status: 'PendingApproval' }),
        getMarinaInvoices(marinaId, { status: 'Overdue' }),
        getMarinaMaintenanceRequests(marinaId, { status: 'InProgress' }),
      ]);
      return {
        pendingReservations: res.length,
        overdueInvoices: inv.length,
        openWorkOrders: wo.length,
      };
    },
    staleTime: 60_000,
  });
}
```

The rail invalidates this query when any of the underlying screens mutate data
(approve a reservation, mark an invoice paid, close a work order). Use TanStack
Query's `invalidateQueries(['marina-counters', marinaId])` in the relevant
mutation `onSuccess` callbacks.

## Responsive contract

**Container queries, not media queries.** The workspace root has:

```tsx
<div className="@container w-full h-full">
  …
</div>
```

(Tailwind 4 supports container queries via `@container` and breakpoint variants
like `@xl:` natively. If you'd rather write plain CSS, the equivalent is
`container-type: inline-size; container-name: workspace`.)

Three breakpoints:

| Width | Form | Markup |
|---|---|---|
| ≥ 1024 px | Full sidebar (240 px) with group section labels | `<MarinaRail />` |
| 720–1023 px | Icon-only rail (64 px) with group dividers | same `<MarinaRail />` with `data-collapsed` |
| < 720 px | Bottom tab bar (5 items) | `<MarinaTabBar />` |

The same `MarinaWorkspaceLayout` renders both `<MarinaRail />` and
`<MarinaTabBar />`; the container query hides whichever is wrong for the current
width:

```css
@container workspace (max-width: 719px) {
  .marina-rail { display: none; }
  .marina-tabbar { display: grid; }
}
@container workspace (min-width: 720px) {
  .marina-tabbar { display: none; }
}
```

Why container queries: the workspace also renders inside the in-app prototype
preview (where the viewport is 1380 px but the workspace itself is constrained to
390 px). A media-query breakpoint would lie. Container queries always tell the
truth.

## Active state

The sidebar reads `useLocation().pathname` and matches the segment after
`/marina/:marinaId/`. It does **not** consider search params. So
`/marina/abc/maintenance?col=inprogress` highlights Maintenance — the column
filter is irrelevant to the sidebar's active state.

## Page header + body

Every child route renders into the same scaffold:

```tsx
<>
  <PageHeader title="Reservations" subtitle="…" actions={<Button>…</Button>} />
  <PageBody>
    {/* screen content */}
  </PageBody>
</>
```

`PageHeader` has padding `20px 24px 0`, optional `<PageTabs>` slot underneath the
title row (used by Settings sub-tabs). `PageBody` has padding `24px` and is the
only scroll container — never let an inner card scroll.

## Membership / permission guard

The mega-page today guards on Membership at mount. Move that guard to
`MarinaWorkspaceLayout` so every child route is protected by one check:

```tsx
export function MarinaWorkspaceLayout() {
  const { marinaId } = Route.useParams();
  const { marinaMemberships } = useAuthStore();
  const hasAccess = marinaMemberships().some((m) => m.marinaId === marinaId);

  if (!hasAccess) {
    return <Navigate to="/" />;
  }

  return (
    <div className="@container ...">
      <MarinaRail marinaId={marinaId} />
      <main><Outlet /></main>
      <MarinaTabBar marinaId={marinaId} />
    </div>
  );
}
```

`NavBar` is rendered by the `__root__` route, not by the workspace layout. It
already exists on every operator URL because of the existing layout.
