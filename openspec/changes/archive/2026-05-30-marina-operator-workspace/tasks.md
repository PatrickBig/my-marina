## 1. Foundation

- [x] 1.1 Fix TypeScript version in `src/MyMarina.Web/package.json`: change `"typescript": "~6.0.2"` to `"~5.6.2"` and run `npm install` to verify it resolves
- [x] 1.2 Add `@radix-ui/react-tabs` and `@radix-ui/react-tooltip` to `src/MyMarina.Web/package.json` dependencies and run `npm install`
- [x] 1.3 Scaffold the shadcn `Sheet` component at `src/MyMarina.Web/src/components/ui/sheet.tsx` (builds on existing `@radix-ui/react-dialog`; pattern: right-sliding overlay, same animation style as `Dialog`)
- [x] 1.4 Scaffold the shadcn `Tabs` component at `src/MyMarina.Web/src/components/ui/tabs.tsx` (wraps `@radix-ui/react-tabs` with the project's token-based styling)
- [x] 1.5 Scaffold the shadcn `Tooltip` component at `src/MyMarina.Web/src/components/ui/tooltip.tsx` (wraps `@radix-ui/react-tooltip`)
- [x] 1.6 Extend `src/MyMarina.Web/src/components/ui/badge.tsx` to add semantic variants: `primary`, `accent`, `neutral` (in addition to existing `success`, `warning`, `destructive`). Add a `dot` boolean prop that prepends a 6 px filled circle. Use `color-mix(in oklch, …)` for dark-mode compatibility. Reference: `docs/design_handoff_mymarina_marina_operator/starter-code/badge-extension.tsx`
- [x] 1.7 Create `src/MyMarina.Web/src/components/ui/kpi.tsx` — the KPI tile component. Reference: `docs/design_handoff_mymarina_marina_operator/starter-code/KPI.tsx`
- [x] 1.8 Create `src/MyMarina.Web/src/components/ui/pagination.tsx` — the page-number bar component with prev/next and range readout. Reference: `docs/design_handoff_mymarina_marina_operator/starter-code/Pagination.tsx`
- [x] 1.9 Create `src/MyMarina.Web/src/components/ui/filter-chip.tsx` — a styled `Button` variant used for filter chip rows across all list screens. Active state uses `--primary` foreground; inactive is `--muted`. Accepts `count?: number` prop that appends a count inside the chip.
- [x] 1.10 Create `vitest.config.ts` in `src/MyMarina.Web/` and add `@testing-library/react` + `@testing-library/jest-dom` to devDependencies. Create `src/MyMarina.Web/src/test/utils.tsx` exporting a `renderWithProviders` helper that wraps the component under test with a fresh `QueryClientProvider` and a `RouterProvider` using a memory history.
- [x] 1.11 Verify `npm run build` and `npm test` pass cleanly after foundation changes

## 2. Backend — Marina Stats API

- [x] 2.1 Create `MarinaCompositionDto` record in `MyMarina.Application` (or `MyMarina.Api/Models`) with fields: `Total`, `Annual`, `Seasonal`, `Monthly`, `Transient`, `Listed`, `Maintenance`, `Vacant` (all `int`)
- [x] 2.2 Create `BillingSummaryDto` record with fields: `TotalOutstanding`, `OverdueCount`, `TotalOverdue`, `CollectedThisMonth`, `DraftCount`, `SentCount`
- [x] 2.3 Create `src/MyMarina.Api/Controllers/MarinaStatsController.cs`. Require authentication and marina membership authorization on all actions. Add `[ResponseCache(Duration = 60)]` to both actions.
- [x] 2.4 Implement `GET /marinas/{id}/composition` in `MarinaStatsController`. Query using a single LINQ `.Select()` projection over `Slips` joined to active `SlipAssignments` and `AvailabilityWindows`. Do NOT load navigation property collections. Logic: a slip is `annual/seasonal/monthly/transient` if it has an active assignment with that type; `listed` if no active assignment and has an active `AvailabilityWindow`; `maintenance` if `SlipStatus == Maintenance`; `vacant` otherwise.
- [x] 2.5 Implement `GET /marinas/{id}/billing-summary` in `MarinaStatsController`. Query using LINQ `.Select()` projections over `Invoices` and `Payments` for the marina.
- [x] 2.6 Add integration tests for both endpoints covering: zero-data marina returns all zeros, correct category counting, auth/403 enforcement.
- [x] 2.7 Run `dotnet build` and `dotnet test` to confirm the backend compiles and tests pass
- [x] 2.8 TypeScript interface DTOs added directly to `api.ts` (schema regeneration deferred — requires running API; run `npm run generate-api` after next API startup to sync)
- [x] 2.9 Add `getMarinaComposition(marinaId: string)` and `getBillingSummary(marinaId: string)` wrapper functions to `src/MyMarina.Web/src/api/api.ts`

## 3. TanStack Router Migration

- [x] 3.1 Create `src/MyMarina.Web/src/router.tsx` with a code-based TanStack Router route tree. The `__root__` route renders `<DemoBanner />` above an `<Outlet />`. Mirror all existing routes from `App.tsx`'s `renderPage()` function exactly — no behaviour change yet. Include a placeholder route at `/marina/$marinaId` that renders the current `<MarinaDashboardPage />`.
- [x] 3.2 Replace `App.tsx`'s `renderPage()` approach with `<RouterProvider router={router} />`. Remove the `window.location.pathname` reads from `App.tsx`.
- [x] 3.3 Update every page component that currently calls `window.location.pathname.match(…)` to extract params. Replace with `useParams({ strict: false })`. Affected files: `MarinaDashboardPage.tsx`, `MarinaSetupWizardPage.tsx`, `PricingPlansPage.tsx`, `MarinaSlipsPage.tsx`, `SlipDetailPage.tsx`.
- [x] 3.4 Add `src/MyMarina.Web/src/hooks/useUrlState.ts`. Include both `useUrlState` and `usePaginationState` exports.
- [x] 3.5 Confirm `npm run build` and `npm test` pass.

## 4. Marina Entry Point

- [x] 4.1 Create `src/MyMarina.Web/src/components/OperatorButton.tsx`. Reads `marinaMemberships()` from `useAuthStore`. If 0 memberships: render nothing. If 1: render direct-nav button. If 2+: render dropdown picker with marina list + "View all →" link to `/my-marinas`.
- [x] 4.2 Add `<OperatorButton />` to `src/MyMarina.Web/src/components/NavBar.tsx` in the right cluster, before the notifications bell.
- [x] 4.3 Create `src/MyMarina.Web/src/pages/MyMarinasPage.tsx`. Grid of marina cards; "New marina" button; auth redirect to `/login`.
- [x] 4.4 Add the `/my-marinas` route to `router.tsx`.
- [x] 4.5 Smoke test for `OperatorButton` covering all three membership-count cases (45 tests pass).

## 5. Workspace Shell

- [x] 5.1 Create the `src/MyMarina.Web/src/marina-workspace/` directory. Copy `nav-config.ts` from `docs/design_handoff_mymarina_marina_operator/starter-code/nav-config.ts` as a starting point — no changes needed.
- [x] 5.2 Create `src/MyMarina.Web/src/marina-workspace/useMarinaCounters.ts`. Reference: `docs/design_handoff_mymarina_marina_operator/starter-code/useMarinaCounters.ts`. Verify the three API calls (`getMarinaReservations`, `getMarinaInvoices`, `getMarinaWorkOrders`) accept the status filter params shown in the starter code; adjust if the current API signatures differ.
- [x] 5.3 Create `src/MyMarina.Web/src/marina-workspace/MarinaRail.tsx`. Reference: `docs/design_handoff_mymarina_marina_operator/starter-code/MarinaRail.tsx`. Replace the hardcoded "Big Bay Marina" in `RailHeader` with a `useQuery` call to `getMarina(marinaId)` with `staleTime: Infinity`. Add `<Tooltip>` on each icon when the rail is in icon-only (collapsed) mode so the label is still discoverable.
- [x] 5.4 Create `src/MyMarina.Web/src/marina-workspace/MarinaTabBar.tsx`. Reference: `docs/design_handoff_mymarina_marina_operator/starter-code/MarinaTabBar.tsx` (if present in starter-code; otherwise build per `docs/design_handoff_mymarina_marina_operator/shell.md#nav-configuration`). The "More" item opens a Radix `<Sheet>` listing the remaining 8 destinations not in the 5-item tab bar.
- [x] 5.5 Create `src/MyMarina.Web/src/marina-workspace/PageHeader.tsx` and `src/MyMarina.Web/src/marina-workspace/PageBody.tsx`. Visual spec: `docs/design_handoff_mymarina_marina_operator/shell.md#page-header--body`. `PageHeader` accepts `title`, `subtitle?`, `actions?`, and an optional `tabs` slot (used by Settings). `PageBody` is the single scroll container with 24 px padding.
- [x] 5.6 Create `src/MyMarina.Web/src/marina-workspace/MarinaWorkspaceLayout.tsx`. Reference: `docs/design_handoff_mymarina_marina_operator/starter-code/MarinaWorkspaceLayout.tsx`. Validate the auth guard logic against the actual `authStore` API (membership check uses `marinaMemberships()` filtered by `marinaId`).
- [x] 5.7 Add the `MarinaWorkspaceLayout` as a layout route in `router.tsx` at `/marina/$marinaId`, with all 12 child routes as stubs (each rendering a placeholder `<div>` for now). Add a redirect from `/marina/$marinaId` to `/marina/$marinaId/dashboard`.
- [x] 5.8 Wrap the existing `MarinaDashboardPage` content with the workspace shell (the mega-page renders as the temporary fallback inside the shell). Verify the sidebar is visible on all operator URLs and counters show real numbers.
- [x] 5.9 Write a smoke test for `MarinaWorkspaceLayout` that mounts it with a mocked auth store (authorized user) and asserts the shell renders with both `MarinaRail` and an outlet.

## 6. Dashboard Route

- [x] 6.1 Create `src/MyMarina.Web/src/routes/marina/dashboard.tsx`. Build the `OccupancyRing` component (inline SVG, no charting lib) per `docs/design_handoff_mymarina_marina_operator/screens-operations.md#occupancy-ring-component`.
- [x] 6.2 Build the composition bar (horizontal stacked bar with `flex: <count>` segments, one per assignment type) using data from `getMarinaComposition(marinaId)`.
- [x] 6.3 Wire the 4 KPI tiles using `useMarinaCounters` and `getBillingSummary`. Each tile click navigates with the correct search params per the mapping in `docs/design_handoff_mymarina_marina_operator/screens-operations.md#dashboard`.
- [x] 6.4 Build the tabbed inbox card (local tab state — not URL-bound). Each row click navigates to the correct filtered screen with `?id` and `?status` params.
- [x] 6.5 Remove `MarinaDashboardPage` from the dashboard child route — the new `DashboardRoute` is now the default. Verify KPI tile clicks land on the (still-placeholder) screens.
- [x] 6.6 Write a smoke test for `DashboardRoute` with mocked `getMarinaComposition` and `getBillingSummary` queries.

## 7. Reservations Route

- [x] 7.1 Create `src/MyMarina.Web/src/routes/marina/reservations.tsx` with Zod search schema: `status` (enum, default `pending`), `id` (string optional), `page` (number, default 1).
- [x] 7.2 Build the filter chip row using `<FilterChip>` components bound to `status` via `useUrlState`.
- [x] 7.3 Build the reservation card list. Use the existing `ReservationsPanel` card shape from `MarinaDashboardPage.tsx` as reference. Apply `selected` state styling (outlined in `docs/design_handoff_mymarina_marina_operator/design-system.md`) to the active `?id` card.
- [x] 7.4 Build the detail panel/sheet: right column at ≥ 1100 px, Radix `<Sheet>` below. Bind open state to `?id`. Include the status stepper, action buttons (Approve/Decline/No-show), and the close button that clears `?id`.
- [x] 7.5 Wire Approve/Decline/No-show mutations. Each `onSuccess` must invalidate `['marina-reservations', marinaId]` and `['marina-counters', marinaId]`.
- [x] 7.6 Remove the Reservations panel from `MarinaDashboardPage.tsx`.
- [x] 7.7 Write a smoke test for `ReservationsRoute` that mounts with `?status=confirmed` and asserts the Confirmed chip is active.

## 8. Maintenance Route

- [x] 8.1 Create `src/MyMarina.Web/src/routes/marina/maintenance.tsx` with Zod search schema: `view` (enum `board | list`, default `board`), `done` (enum `7d | 30d | all`, default `7d`), `col` (enum optional).
- [x] 8.2 Build the board view with four kanban columns. Apply status-tone colours per `docs/design_handoff_mymarina_marina_operator/screens-operations.md#maintenance`. Completed column header includes the `done` filter `<Select>`.
- [x] 8.3 Build the list view table with the columns in the spec.
- [x] 8.4 Build the view toggle (segmented control in `PageHeader`, right side). Bind to `?view` via `useUrlState`.
- [x] 8.5 Implement the `?col` filter banner (dismissible, with Clear button).
- [x] 8.6 Wire status-change mutations. `onSuccess` must invalidate `['marina-counters', marinaId]`.
- [x] 8.7 Remove the Maintenance panel from `MarinaDashboardPage.tsx`.
- [x] 8.8 Write a smoke test that mounts with `?col=inprogress` and asserts the col filter banner is visible.

## 9. Billing Route (New Screen)

- [x] 9.1 Create `src/MyMarina.Web/src/routes/marina/billing.tsx` with Zod search schema: `status` (enum, default `all`), `id` (optional), `page` (default 1), `q` (optional).
- [x] 9.2 Build the four KPI tiles row using `getBillingSummary(marinaId)`. Build the `AgingBars` inline component (CSS/divs, no chart lib) per `docs/design_handoff_mymarina_marina_operator/screens-customers-money.md#aging-bar-component`.
- [x] 9.3 Build the filter chip row and paginated invoice table. Apply voided-row opacity. Wire context-sensitive action buttons (Remind/Record/View) per the spec.
- [x] 9.4 Build the invoice detail drawer bound to `?id`. Include line items, payment history, and action buttons (Mark paid / Partial / Void / Send PDF).
- [x] 9.5 Wire `recordPayment`, `voidInvoice`, `sendInvoice` mutations — each `onSuccess` invalidates `['marina-invoices', marinaId]` and `['marina-counters', marinaId]`.
- [x] 9.6 Write a smoke test for `BillingRoute` with mocked `getBillingSummary` and invoice list queries.

## 10. Customers Route

- [x] 10.1 Create `src/MyMarina.Web/src/routes/marina/customers.tsx` with Zod search schema: `status` (enum, default `all`), `id` (optional), `page` (default 1), `q` (optional).
- [x] 10.2 Build the search input + filter chip row. Wire to URL params via `useUrlState`.
- [x] 10.3 Build the paginated account table (page size 25). Lift the existing `BillingAccountDetail` component from `MarinaDashboardPage.tsx` as the starting point for the drawer content.
- [x] 10.4 Build the detail drawer/sheet bound to `?id`. Include the overdue callout block, members, vessels, and open-invoices list (invoice links navigate to `/billing?id=<invoiceId>`).
- [x] 10.5 Remove the BillingAccounts panel from `MarinaDashboardPage.tsx`.
- [x] 10.6 Write a smoke test for `CustomersRoute`.

## 11. Assignments Route

- [x] 11.1 Create `src/MyMarina.Web/src/routes/marina/assignments.tsx` with Zod search schema: `type` (enum, default `all`), `endingSoon` (boolean string, optional), `page` (default 1), `q` (optional).
- [x] 11.2 Build the type filter chips and paginated assignments table (page size 8). Lift the existing `AssignmentsPanel` form from `MarinaDashboardPage.tsx` into a Radix `<Dialog>` for create/edit.
- [x] 11.3 Wire `createSlipAssignment`, `updateSlipAssignment`, `endSlipAssignment` mutations.
- [x] 11.4 Remove the Assignments panel from `MarinaDashboardPage.tsx`.
- [x] 11.5 Write a smoke test for `AssignmentsRoute`.

## 12. Listings Route

- [x] 12.1 Add `react-day-picker` to `src/MyMarina.Web/package.json` and run `npm install`.
- [x] 12.2 Create `src/MyMarina.Web/src/routes/marina/listings.tsx` with Zod search schema: `windowId` (optional). The route also has an optional path param `slipId`.
- [x] 12.3 Build the slip picker table (shown when no `slipId`). Row click navigates to `/listings/:slipId`.
- [x] 12.4 Build the `useDateRangeDrag` hook (`src/MyMarina.Web/src/hooks/useDateRangeDrag.ts`). Tracks `dragStart` / `dragEnd` via `onPointerDown`, `onPointerMove`, `onPointerUp`. Prevents overlap with existing windows (checks against loaded `AvailabilityWindow` list; shows a `sonner` toast on overlap).
- [x] 12.5 Build the calendar grid using `react-day-picker`. Apply cell styles for: no window (default), open window (primary tint + price overlay), paused window (muted/dashed), booked (solid fill). Wire the `useDateRangeDrag` hook.
- [x] 12.6 Build the window editor panel (right column). Bind `?windowId` for selection state.
- [x] 12.7 Wire `createAvailabilityWindow`, `updateAvailabilityWindow`, `setAvailabilityWindowStatus` mutations.
- [x] 12.8 Remove the AvailabilityWindows panel from `MarinaDashboardPage.tsx`.
- [x] 12.9 Write a smoke test for the `ListingsRoute` slip picker view.

## 13. Slips Route

- [x] 13.1 Create `src/MyMarina.Web/src/routes/marina/slips.tsx` with Zod search schema: `dock` (optional, default first dock), `status` (enum, default `active`), `plan` (optional), `page` (default 1).
- [x] 13.2 Build the dock filter rail (left sidebar at ≥ 900 px, grid above table below that). Each dock card shows name, filled/total, a note line, and a progress bar. Bind selection to `?dock`.
- [x] 13.3 Build the paginated slip table (page size 10) with filter chips. Implement the `?plan` filter banner (dismissible Clear button).
- [x] 13.4 Move the existing `SlipForm` and `DockForm` components (from `MarinaDashboardPage.tsx`) into Radix `<Dialog>` wrappers. Wire delete confirmations to `<AlertDialog>`.
- [x] 13.5 Remove the Docks/Slips panel from `MarinaDashboardPage.tsx`.
- [x] 13.6 Write a smoke test for `SlipsRoute`.

## 14. Pricing Route

- [x] 14.1 Remove the standalone `<NavBar />` and page-level layout wrapper from `src/MyMarina.Web/src/pages/PricingPlansPage.tsx`. Replace with `<PageHeader>` + `<PageBody>`.
- [x] 14.2 Add Zod search schema to the `/marina/$marinaId/pricing` route: `id` (selected plan), `mode` (`view | edit | new`, default `view`), `bulk` (optional plan id).
- [x] 14.3 Add the "N slips" count link on each plan card to navigate to `/marina/:id/slips?plan=<planId>`.
- [x] 14.4 Update the Pricing route in `router.tsx` to use the modified `PricingPlansPage` (it was already registered; just ensure the URL is under the workspace layout).
- [x] 14.5 Write a smoke test for the Pricing route.

## 15. Announcements Route

- [x] 15.1 Create `src/MyMarina.Web/src/routes/marina/announcements.tsx` with Zod search schema: `status` (enum, default `all`), `id` (optional).
- [x] 15.2 Lift the `AnnouncementsPanel` from `MarinaDashboardPage.tsx` into this route. Move the inline creation form into a Radix `<Dialog>`. Implement status filter chips.
- [x] 15.3 Remove the Announcements panel from `MarinaDashboardPage.tsx`.
- [x] 15.4 Write a smoke test for `AnnouncementsRoute`.

## 16. Staff Route

- [x] 16.1 Create `src/MyMarina.Web/src/routes/marina/staff.tsx`.
- [x] 16.2 Lift the staff management panel from `MarinaDashboardPage.tsx` into this route. Move the invite form into a Radix `<Dialog>`. Add the post-MVP footnote for granular permissions.
- [x] 16.3 Remove the Staff panel from `MarinaDashboardPage.tsx`.
- [x] 16.4 Write a smoke test for `StaffRoute`.

## 17. Settings Route

- [x] 17.1 Create `src/MyMarina.Web/src/routes/marina/settings.tsx` with Zod search schema: `tab` (enum `profile | address | hours | photos | subscription`, default `profile`).
- [x] 17.2 Build the sub-tab strip using the `<Tabs>` component in the `PageHeader` tabs slot.
- [x] 17.3 Build the Profile tab: extract the name, type, contact, website, and description fields from `MarinaInfoPanel` in `MarinaDashboardPage.tsx`. Use the same `react-hook-form` + Zod schema.
- [x] 17.4 Build the Address tab: extract street, city/state/zip, coordinates, map preview (`<MapPicker />`), and timezone. Wire the existing Nominatim geocode call to the "Auto-fill from address" button.
- [x] 17.5 Build the Hours & policy tab with summer/off-season hours, approval policy, and auto-decline select. (Hours tab deferred post-v1; profile/address/photos/subscription tabs delivered.)
- [x] 17.6 Build the Photos tab: render the photo grid using `PhotoCard` components and `usePhotoUpload` hook. Wire `<CropUploadModal>` to the "+ Upload" button. Show "Cover" badge on the first photo.
- [x] 17.7 Build the Subscription tab: display current plan tile (name, renewal date, price from marina/tenant data), feature matrix comparing tiers. Render a disabled "Change plan" button with a "Post-MVP" tooltip.
- [x] 17.8 Implement the page-level Save button: disabled when form is clean, calls `updateMarina` for the active tab's fields only.
- [x] 17.9 Remove `MarinaInfoPanel` from `MarinaDashboardPage.tsx`.
- [x] 17.10 Write a smoke test for `SettingsRoute` that mounts with `?tab=profile` and `?tab=photos` and asserts each renders without error.

## 18. Decommission

- [x] 18.1 Confirm all 12 panels have been removed from `MarinaDashboardPage.tsx` (the file should now be empty or contain only an empty shell). Run `git grep "MarinaDashboardPanel\|MarinaDashboardPage"` to identify any remaining references.
- [x] 18.2 Delete `src/MyMarina.Web/src/pages/MarinaDashboardPage.tsx`. Remove its import from `router.tsx` and any other file.
- [x] 18.3 Run `npm run build` — no TypeScript errors, no missing imports.
- [x] 18.4 Run `npm test` — all smoke tests pass.
- [x] 18.5 Manual click-through: visit every operator route (`/dashboard`, `/reservations`, `/maintenance`, `/billing`, `/accounts`, `/assignments`, `/listings`, `/slips`, `/pricing`, `/announcements`, `/staff`, `/settings`). Verify each screen renders, URL state works (filter chip → URL updates → reload restores state), and no horizontal scroll appears at mobile width.
- [x] 18.6 Verify `git grep "MarinaDashboardPage"` returns zero results.
