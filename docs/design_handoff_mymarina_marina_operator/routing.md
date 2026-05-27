# Routing & URL-as-state

This is the most important behavioural change in the brief. Read it first.

## The contract

> **Every filter, tab, selection, drawer, and page number that the operator can
> change must be reflected in the URL, and the page must restore itself from the
> URL on cold load.**

This is non-negotiable. It enables three things at once:

1. Operators can share links to filtered views ("here's the overdue invoices for
   the Lee family — `/marina/abc/billing?status=overdue&id=acct_lee`").
2. The browser back/forward buttons work the way users expect.
3. We never need to invent another "state preservation" mechanism — sessionStorage,
   in-memory caches — because reloading the page is always free.

## URL shape

```
/marina/:marinaId/<screen>?<key1>=<v1>&<key2>=<v2>…
```

| Segment | Meaning |
|---|---|
| `:marinaId` | Existing path param — unchanged. |
| `<screen>` | The route under the workspace shell. Members of the list in `shell.md`. |
| query params | Per-screen filter / selection / pagination state. |

### Canonical query parameter names

These are shared across screens — Claude Code should not invent new names where one
of these applies.

| Param | Meaning | Example |
|---|---|---|
| `status` | The currently active filter chip / tab. | `?status=pending` |
| `id` | The currently selected row (drawer open). | `?id=acct_lee` |
| `page` | 1-indexed current page. Omit when on page 1. | `?page=3` |
| `view` | Visual mode toggle (board vs list, list vs map). | `?view=list` |
| `col` | Kanban column to scope to (Maintenance). | `?col=inprogress` |
| `done` | Time range for completed items (Maintenance). | `?done=30d` |
| `tab` | Sub-tab within a screen (Settings). | `?tab=photos` |
| `q` | Free-text search query. | `?q=lee` |
| `plan` | Pricing plan id (Slips, when filtering by plan). | `?plan=p1` |

## Choice of router

The repo already has `@tanstack/react-router` in `package.json` but routing is
hand-rolled in `App.tsx` (a giant `if (path === '…')` ladder). **Switch to TanStack
Router for this work.** Reasons:

- The hand-rolled router can't model nested routes with shared layouts cleanly,
  which is exactly the shape we need (workspace shell + child route).
- TanStack Router has first-class typed search-params with Zod validation. That's
  the perfect fit for URL-as-state.
- It's already a dependency. No new packages.

### File-based or code-based?

**Use code-based routing.** Reasons:

- The existing app uses Vite without a routes-directory convention, and onboarding
  the codegen step is a bigger lift than the routes themselves.
- We have ~14 routes total — code-based is fine at that scale.
- It keeps everything visible in one place (`src/MyMarina.Web/src/router.ts`).

## Route tree

```
__root__                                     (existing app root + <NavBar />)
├── /                                        HomePage
├── /login                                   LoginPage
├── /auth/callback                           AuthCallbackPage
├── /search                                  SearchPage
├── /search/marinas/$marinaId                MarinaSlipsPage
├── /slips/$slipId                           SlipDetailPage
├── /trips                                   MyTripsPage
├── /my-slips                                MySlipsPage
├── /invoices                                MyInvoicesPage
├── /maintenance                             MaintenancePage
├── /boats                                   MyBoatsPage
├── /profile                                 ProfilePage
├── /admin                                   PlatformOperatorPage
├── /marina/new                              MarinaOnboardingPage
├── /dock/new                                PrivateDockOnboardingPage
├── /slip/new                                DockominionOnboardingPage
└── /marina/$marinaId                        MarinaWorkspaceLayout   ← NEW
    ├── /                                    redirect → /dashboard
    ├── /dashboard                           DashboardRoute
    ├── /reservations                        ReservationsRoute
    ├── /maintenance                         MaintenanceRoute
    ├── /listings                            ListingsRoute
    ├── /accounts                            CustomersRoute
    ├── /assignments                         AssignmentsRoute
    ├── /billing                             BillingRoute
    ├── /slips                               SlipsRoute
    ├── /pricing                             (existing PricingPlansPage, moved here)
    ├── /announcements                       AnnouncementsRoute
    ├── /staff                               StaffRoute
    └── /settings                            SettingsRoute
        ├── /                                redirect → ./profile
        ├── /profile
        ├── /address
        ├── /hours
        ├── /photos
        └── /subscription
```

Existing routes outside `/marina/:id/*` keep their current shape and component
files. Only the `MarinaDashboardPage` ladder collapses.

## Search-param typing

Each route declares its search schema with Zod. Example for Reservations:

```ts
// src/routes/marina/$marinaId/reservations.ts
import { createFileRoute } from '@tanstack/react-router';
import { z } from 'zod';

const searchSchema = z.object({
  status: z.enum(['all','pending','confirmed','today','past','cancelled']).default('pending'),
  id:     z.string().optional(),
  page:   z.number().int().min(1).default(1),
});

export const Route = createFileRoute('/marina/$marinaId/reservations')({
  validateSearch: searchSchema,
  component: ReservationsRoute,
});
```

(If using code-based routing, the same `validateSearch` lives on the route object.)

Inside the component, read + write search params with `useSearch()` and
`navigate({ search })`:

```ts
const { status, id, page } = Route.useSearch();
const navigate = Route.useNavigate();

const setStatus = (next: typeof status) =>
  navigate({ search: (prev) => ({ ...prev, status: next, page: 1 }) });
```

Always reset `page` to 1 when changing filters or searches. This is a UX rule.

## `useUrlState` helper

For consistency across screens, wrap the get/set pair in a hook. See
[`starter-code/useUrlState.ts`](./starter-code/useUrlState.ts).

## Dashboard widget navigation

KPI tiles and inbox rows on the dashboard navigate by **routing with search
params**, not by setting state on the dashboard itself. The Maintenance "Open work
orders" tile, for example, is just:

```tsx
const navigate = useRouter().navigate;
<KPI
  label="Open work orders" value="7"
  onClick={() => navigate({
    to: '/marina/$marinaId/maintenance',
    params: { marinaId },
    search: { col: 'inprogress' },
  })}
/>
```

The receiving screen reads `col` from its search params and applies the filter.
**No dashboard-to-screen prop passing, no global event bus, no Zustand for filter
state.** The URL is the bus.

## Reset behaviour

When the user navigates via the sidebar (clicking a fresh destination), the search
params reset to empty — the receiving screen uses its declared defaults. This
matches the prototype.

The exception is **the sidebar's active state**: it reads `pathname`, not `search`,
so navigating to Maintenance with a column filter still shows Maintenance as
active.

## Test acceptance

For every screen with URL state, add a smoke test that:

1. Mounts the screen with a specific search-param shape.
2. Asserts that the corresponding UI state matches.
3. Triggers a state change (click a filter chip).
4. Asserts the URL has been updated via the router's history API.

This is the closest you get to a contract for the URL ↔ UI relationship.
