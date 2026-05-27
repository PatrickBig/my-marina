## Context

The marina operator surface currently lives entirely in `MarinaDashboardPage.tsx` (3,021 lines). It stacks 12 unrelated panels on one route, manages all their state locally with `useState`, and uses no routing primitives — `window.location.pathname` is read once on render and never updated.

TanStack Router is already in `package.json` (`^1.168.21`) but completely unused. `App.tsx` uses a hand-rolled `if/else` path ladder and `window.location.pathname` directly. Every page that needs a path param does its own `pathname.match(…)` regex.

The visual spec and interaction design for all screens lives in the handoff package at `docs/design_handoff_mymarina_marina_operator/`. That package is the authoritative source for layout, component shapes, and responsive breakpoints. This design doc focuses on architecture decisions, not screen-level details.

## Goals / Non-Goals

**Goals:**
- Replace the mega-page with 12 focused route components under a shared workspace shell
- Establish URL-as-state as the only mechanism for filter/selection/pagination — no sessionStorage, no Zustand for UI state
- Make the operator surface fully usable on mobile (bottom tab bar) and tablet (icon rail)
- Add two aggregate backend endpoints that the dashboard and billing screens depend on
- Keep every intermediate state shippable — the app must work after each phase completes

**Non-Goals:**
- Server-side API changes beyond the two new stats endpoints
- Authentication/authorization changes — `Membership` model and JWT shape stay as-is
- Boater-side marketplace routes — untouched
- Stripe Connect / online payments — Era 2
- Kanban drag-and-drop on Maintenance — deferred post-v1
- Platform subscription management UI — Settings/Subscription tab is read-only

## Decisions

### D1 — TanStack Router (code-based), not file-based and not hand-rolled

**Chosen:** Code-based TanStack Router in `src/MyMarina.Web/src/router.tsx`.

The workspace needs nested layout routes (shell + child) and typed Zod search params for URL state. TanStack Router provides both. File-based routing was rejected because it requires Vite codegen integration and a routes-directory convention neither the current codebase nor the team uses. The hand-rolled ladder was rejected because it cannot model nested layouts cleanly and has no search-param typing.

With ~16 total routes, code-based is readable at this scale and keeps everything visible in one file.

### D2 — URL carries all operator UI state (no Zustand for filter/selection)

**Chosen:** Every active tab, filter chip, selected row (drawer open), view toggle, and page number is a Zod-validated search param on the route.

This is the single biggest behavioral change. Benefits: shareable links, back-button works, reload is free. Cost: slightly more verbose component code (navigate instead of setState). The `useUrlState(key, default)` hook wrapper in the starter code keeps call sites clean. Zustand is already used for auth; it stays for auth only.

### D3 — Backend stats endpoints before dashboard/billing frontend

**Chosen:** Implement `MarinaStatsController` first, then the two frontend screens that depend on it.

Alternative considered: derive aggregates client-side (download all invoices/slips, compute in the browser). Rejected because the stats endpoints are the right long-term shape, and building the frontend against a proper endpoint prevents a later breaking refactor. Both endpoints are pure read-only LINQ projections with no new migration required.

**Endpoint shapes:**

`GET /marinas/{id}/composition` → `MarinaCompositionDto`:
```
{ total, annual, seasonal, monthly, transient, listed, maintenance, vacant }
```
Derived from a single LINQ query joining `Slips` to their active `SlipAssignment` (if any) and `AvailabilityWindow` (to distinguish "listed" from "vacant"). Use `.Select()` projection — do not load navigation properties.

`GET /marinas/{id}/billing-summary` → `BillingSummaryDto`:
```
{ outstandingAmount, outstandingCount, overdueAmount, overdueCount,
  oldestOverdueDays, mtdCollected, lastMonthCollected,
  agingBuckets: { current, days1to30, days31to60, days60plus } }
```
Derived from `Invoices` and `Payments` for the marina. Again `.Select()` projection only.

Both endpoints get `[ResponseCache(Duration = 60)]`. No migrations required — read-only over existing tables.

### D4 — Container queries for workspace responsive breakpoints

**Chosen:** CSS container queries on the workspace root (`@container/workspace`), not viewport media queries.

The workspace also renders inside the in-app prototype preview where viewport width doesn't correspond to the workspace's actual width. Container queries always tell the truth. Tailwind 4 (already installed) supports `@container` and `@max-[Npx]/workspace:` variants natively.

Three breakpoints:
- `≥ 1024 px` — full sidebar (240 px) with group labels and counters
- `720–1023 px` — icon-only rail (64 px)
- `< 720 px` — bottom tab bar (5 items + "More" sheet)

### D5 — `react-day-picker` + pointer-event drag for Listings calendar

**Chosen:** `react-day-picker` renders the month grid; custom `onPointerDown`/`onPointerMove`/`onPointerUp` handlers layer drag-range selection on top.

FullCalendar was rejected (heavy, ~200 KB, own styling system). A fully custom grid was considered but `react-day-picker` gives us accessible keyboard navigation and locale-aware day labeling for free. The drag interaction is ~80 lines of pointer-event tracking that maintains `dragStart` / `dragEnd` state and highlights the in-progress range.

### D6 — MarinaWorkspaceLayout owns the auth guard

**Chosen:** One membership check in `MarinaWorkspaceLayout`. If `marinaMemberships().some(m => m.marinaId === marinaId)` is false, redirect to `/`.

Alternative: guard each child route individually. Rejected — redundant and easy to miss on new routes. The brief flash of the shell before redirect is acceptable; the auth store is synchronous (loaded from localStorage via Zustand persist), so in practice there is no async flicker.

### D7 — `OperatorButton` in NavBar: smart nav based on membership count

**Chosen:** A single anchor-icon button in the NavBar right section:
- 0 marina memberships → hidden
- 1 marina membership → direct `navigate` to `/marina/:id/dashboard`
- 2+ marina memberships → Radix `DropdownMenu` listing each marina (name + tier badge) plus "View all →" to `/my-marinas`

This means `/my-marinas` is a fallback for multi-marina operators, not the primary entry point. Reduces clicks for the common single-marina case.

### D8 — MarinaRail loads marina name from TanStack Query

**Chosen:** The rail header calls `useQuery({ queryKey: ['marina', marinaId], queryFn: () => getMarina(marinaId) })` to get the name and tier. `staleTime: Infinity` — marina metadata doesn't change during a session.

The starter-code `MarinaRail.tsx` hardcodes `"Big Bay Marina"` — this must be replaced. The query result is already likely cached from the workspace layout mounting (which also calls `getMarina`), so in practice this is a cache hit.

### D9 — `useMarinaCounters` fetches full arrays in v1

**Chosen (temporary):** `useMarinaCounters` makes three parallel requests and counts array lengths. Acceptable for v1 because the new `MarinaStatsController` doesn't cover counts (it covers composition and billing aggregates). A follow-up can add a dedicated counters endpoint. `staleTime: 60_000` reduces polling pressure.

### D10 — Test infrastructure scaffolded in Phase 0

**Chosen:** Add `vitest.config.ts` + `@testing-library/react` + a `renderWithProviders` test utility (wraps with `QueryClientProvider` + `RouterProvider`) before any screens are built. Each route component gets one smoke test minimum: mount with mocked TanStack Query, assert it renders without throwing.

## Risks / Trade-offs

**[Risk] TanStack Router migration touches every existing page** → Every page currently does `window.location.pathname.match(…)` for param extraction. The migration phase (Phase 2) must update all of them to `Route.useParams()`. Risk of regression is real. Mitigation: the acceptance criterion for Phase 2 is that every existing route still loads and all nav links work — manual click-through of the full app before merge.

**[Risk] `MarinaDashboardPage.tsx` has deeply intertwined state** → The mega-page shares state across panels via closures and prop threading. Lifting panels out individually risks breaking cross-panel interactions (e.g., creating a billing account from the slip-assignment form). Mitigation: each panel is extracted into its own route with its own data loading. Cross-panel actions that currently write to shared state become mutations that invalidate TanStack Query keys, which both screens observe.

**[Risk] Listings calendar drag interaction complexity** → The pointer-event drag is more fragile than React state toggles — pointer capture, scroll containers, and touch events all interact. Mitigation: `react-day-picker` handles the DOM structure; the drag layer is isolated in a `useDateRangeDrag` hook with its own unit tests. Touch support (mobile) is not required for v1 (operators manage listings on desktop).

**[Risk] `[ResponseCache(Duration = 60)]` on stats endpoints may serve stale data after mutations** → If an operator assigns a slip while the composition cache is fresh, the dashboard ring won't update for up to 60 seconds. Mitigation: acceptable trade-off for v1. The cache is per-request (ASP.NET output cache), not shared across users. Future improvement: cache invalidation on `SlipAssignment` mutations.

**[Risk] TypeScript `~6.0.2` in package.json** → This version string doesn't correspond to any released TypeScript version. It may install a non-existent version or fall back unexpectedly. Mitigation: corrected to `~5.6.2` in Phase 0 before any build work proceeds.

## Migration Plan

The migration is incremental and the app stays shippable at every phase boundary:

1. **Phase 0** — Foundation: fix TS version, add missing primitives, scaffold test infra, add new UI components. No visible change.
2. **Phase 1** — Backend: add `MarinaStatsController`, regenerate API types. No frontend change yet.
3. **Phase 2** — Router: `App.tsx` swap, all existing routes preserved. No visible change for end users.
4. **Phase 3** — Marina entry: `OperatorButton` in NavBar, `/my-marinas` page. First visible change.
5. **Phase 4** — Workspace shell: `MarinaWorkspaceLayout` wraps the existing `MarinaDashboardPage` content. Sidebar becomes visible everywhere; old panels still render.
6. **Phase 5** — Dashboard: new dashboard replaces the top of the mega-page. Old panels remain as fallback routes.
7. **Phases 6–16** — Per-screen migration (one screen at a time): each panel is lifted into its own route and removed from `MarinaDashboardPage`.
8. **Phase 17** — Decommission: `MarinaDashboardPage.tsx` deleted. `git grep MarinaDashboardPage` returns nothing.

Rollback at any phase: revert the relevant commits. Because each phase leaves the app working, rollback scope is bounded to one screen at a time (Phases 6–16).

## Open Questions

All open questions from `docs/design_handoff_mymarina_marina_operator/open-questions.md` have been resolved during the planning session. Decisions are recorded in this document and in the individual spec files. No outstanding unknowns block implementation.
