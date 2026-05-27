## Why

`MarinaDashboardPage.tsx` is a 3,021-line monolith that renders 12 unrelated panels on a single route with no URL state, no pagination, and zero responsive treatment. Operators scroll endlessly to find anything, can't share links to filtered views, and the file is too large to maintain safely.

## What Changes

- **BREAKING** `/marina/:id` no longer renders `MarinaDashboardPage`. The file is deleted at the end of this work.
- The hand-rolled `if/else` path ladder in `App.tsx` is replaced with TanStack Router (code-based, already installed).
- The operator surface is restructured as a workspace shell (`MarinaWorkspaceLayout`) with 12 child routes, each a focused screen.
- Every filter, tab, selection, and page number is encoded in the URL via Zod-typed search params.
- Two new .NET API endpoints are added: `GET /marinas/{id}/composition` and `GET /marinas/{id}/billing-summary`.
- A new marina entry point is added to `NavBar`: an `OperatorButton` (hidden/direct/dropdown based on marina membership count) and a `/my-marinas` listing page.
- `PricingPlansPage` is absorbed into the workspace shell, losing its standalone `NavBar`.
- A new `Billing` screen is introduced (invoice KPI tiles + aging chart + invoice table) — this panel does not currently exist.
- A new `Settings` screen absorbs `MarinaInfoPanel` and adds Hours, Photos, and Subscription (read-only) tabs.
- `react-day-picker` is added for the Listings availability calendar.

## Capabilities

### New Capabilities

- `marina-operator-workspace`: The workspace shell, responsive left-rail nav, container-query breakpoints, auth guard, `useMarinaCounters` hook, and the shared `PageHeader`/`PageBody` scaffolding all child routes render into.
- `marina-operator-routing`: TanStack Router migration — route tree, typed Zod search params, `useUrlState` / `usePaginationState` hooks, URL-as-state contract for every operator screen.
- `marina-operator-entry`: New `OperatorButton` in `NavBar` (smart nav: direct if 1 marina, dropdown if 2+, hidden if 0) and `/my-marinas` listing page.
- `marina-operator-dashboard`: New dashboard route — occupancy ring, composition bar, 4 KPI tiles, tabbed inbox. KPI tiles deep-link to filtered child routes.
- `marina-operator-reservations`: Lifted from mega-page; URL-bound status tabs, detail drawer with `?id` param, Approve/Decline/No-show actions.
- `marina-operator-maintenance`: Lifted from mega-page; Board ↔ List toggle, Completed date-range filter (`?done`), column filter from dashboard deep-links (`?col`).
- `marina-operator-listings`: Lifted from mega-page; slip picker table + availability calendar editor using `react-day-picker` with pointer-event range drag.
- `marina-operator-customers`: Lifted from mega-page; URL filters, search, right-side detail drawer, pagination.
- `marina-operator-assignments`: Lifted from mega-page; type filter chips, search, pagination.
- `marina-operator-billing`: **New screen** — invoice KPI tiles (outstanding, overdue, MTD collected, aging buckets), status filter chips, paginated invoice table, detail drawer, Record/Remind/Void actions.
- `marina-operator-slips`: Lifted and enhanced; dock filter rail, status filter, `?plan` deep-link from Pricing, pagination.
- `marina-operator-pricing`: Existing `PricingPlansPage` absorbed into workspace shell; plan cards, preview sidebar, bulk-assign dialog.
- `marina-operator-announcements`: Lifted from mega-page; status filter (Published/Draft), pinned-first ordering.
- `marina-operator-staff`: Lifted from mega-page; role/scope table, invite dialog, Revoke/Resend actions.
- `marina-operator-settings`: **New screen** — sub-tabs: Profile, Address & map, Hours & policy, Photos (existing upload flow), Subscription (read-only).
- `marina-stats-api`: Two new .NET endpoints — `GET /marinas/{id}/composition` (slip breakdown by assignment type) and `GET /marinas/{id}/billing-summary` (invoice KPI aggregates). LINQ projections only; `[ResponseCache(Duration = 60)]`.

### Modified Capabilities

- `marina-photo-upload`: The photo upload/manage flow (previously only in the setup wizard) is also surfaced in the new `Settings > Photos` tab. No requirement change to the upload itself — same `CropUploadModal` + `PhotoCard` + `usePhotoUpload` hook — but the capability is now accessible from a second entry point.
- `pricing-preview`: `PricingPreviewPanel` is reused inside the new workspace Pricing screen. No spec-level behavior change; the component moves into the workspace shell context.

## Impact

**Frontend**
- `src/MyMarina.Web/src/App.tsx` — routing ladder replaced
- `src/MyMarina.Web/src/pages/MarinaDashboardPage.tsx` — deleted at end
- `src/MyMarina.Web/src/pages/PricingPlansPage.tsx` — layout replaced (NavBar removed, workspace shell adopted)
- `src/MyMarina.Web/src/components/NavBar.tsx` — `OperatorButton` added
- New directory: `src/MyMarina.Web/src/marina-workspace/`
- New directory: `src/MyMarina.Web/src/routes/marina/`
- New UI components: `KPI`, `Pagination`, `FilterChip`, `Sheet`, semantic `Badge` variants
- New hooks: `useUrlState`, `usePaginationState`
- New dependency: `react-day-picker`
- New Radix primitives: `@radix-ui/react-tabs`, `@radix-ui/react-tooltip`
- TypeScript version corrected: `~6.0.2` → `~5.6.2`

**Backend (.NET)**
- New file: `src/MyMarina.Api/Controllers/MarinaStatsController.cs`
- New DTOs: `MarinaCompositionDto`, `BillingSummaryDto` (in `MyMarina.Application` or `MyMarina.Api`)
- `npm run generate-api` must be re-run after backend changes to update `schema.d.ts`

**No database migrations required.** Both new endpoints are read-only aggregations over existing tables.
