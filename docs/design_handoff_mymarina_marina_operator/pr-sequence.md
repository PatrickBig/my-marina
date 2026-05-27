# PR sequence

Each PR is intended to be:

- Shippable on its own (the app builds and works after every merge).
- Small (~300–800 LOC diff).
- Independently reviewable.

The sequence below is suggested, not load-bearing. If you spot a tighter ordering
mid-project, take it.

---

## PR 1 — Routing foundation

**Goal:** Replace the hand-rolled `App.tsx` ladder with TanStack Router. No
visible UI change.

- [ ] Add `src/MyMarina.Web/src/router.tsx` (or `routeTree.tsx`) with code-based
      routes mirroring today's path matches.
- [ ] Replace `App.tsx`'s `renderPage()` with `<RouterProvider router={router} />`.
- [ ] Each existing page component becomes a route. `MarinaDashboardPage` becomes
      the placeholder route at `/marina/$marinaId` for now — no behaviour change.
- [ ] Add `useUrlState` hook in `src/hooks/`.

**Acceptance**

- All existing routes still load. Manual click-through of every nav link works.
- `npm run build` passes.
- `npm test` passes.

---

## PR 2 — Workspace shell

**Goal:** Build `MarinaWorkspaceLayout` + `MarinaRail` + `MarinaTabBar` +
`PageHeader` / `PageBody`. Wrap the existing `MarinaDashboardPage` with the new
shell so the sidebar is visible everywhere. **The current panels keep rendering
inside the shell — no panel migration yet.**

- [ ] Add `marina-workspace/` folder with shell components.
- [ ] Add `nav-config.ts` and `useMarinaCounters.ts`.
- [ ] Add badge variants in `components/ui/badge.tsx`.
- [ ] Add `KPI.tsx` and `Pagination.tsx`.
- [ ] Wrap `MarinaDashboardPage` so the new sidebar surrounds the old panels.
      Add a temporary "Legacy view" sidebar item if helpful.
- [ ] Counters wire to real API queries.

**Acceptance**

- The sidebar appears with grouped sections. Counters show real numbers.
- Resizing the workspace through 1024 → 720 → 400 px collapses correctly.
- All existing panels still work inside the shell.

---

## PR 3 — Dashboard route

**Goal:** New Dashboard renders at `/marina/$marinaId/dashboard`. KPI tiles and
inbox rows navigate to other (still-on-the-mega-page) routes via search params.

- [ ] New `routes/marina/dashboard.tsx` with the occupancy ring + composition
      bar + tabbed inbox.
- [ ] KPI clicks deep-link.
- [ ] Mega-page becomes the fallback for everything except dashboard.

**Acceptance**

- Default landing for `/marina/$marinaId` is the new dashboard.
- Click any KPI tile — lands on the relevant filtered page (which today is still
  the mega-page; the filter param is parsed but ignored until that route ships).
- Composition bar renders without a charting library.

---

## PRs 4–14 — Per-screen migration

One PR per route. In each PR, lift the existing panel out of `MarinaDashboardPage`
into its own route file, adopt search-param filters, add pagination if it's a
table, and remove the panel from the mega-page.

Suggested order (most-used first):

| PR | Screen | Key adds |
|---|---|---|
| 4 | Reservations | URL-bound `status` tabs, drawer with `?id` |
| 5 | Maintenance | Board ↔ List toggle, Completed `done` filter |
| 6 | Billing (NEW) | KPI tiles, aging chart, status filter, pagination |
| 7 | Customers | URL filters, drawer with `?id`, pagination |
| 8 | Slips | Dock rail, status filter, pagination, `?plan` filter |
| 9 | Assignments | Type filter, pagination |
| 10 | Listings | Slip picker → calendar editor |
| 11 | Pricing plans | Move existing page into workspace shell |
| 12 | Announcements | Lift-and-shift |
| 13 | Staff | Lift-and-shift |
| 14 | Settings | Move `MarinaInfoPanel` here, add Hours / Photos / Subscription tabs |

### Acceptance template for PRs 4–14

- Route exists at the path in `routing.md`. Old panel removed from
  `MarinaDashboardPage`.
- All URL params validated through Zod, defaults match the spec.
- Reload preserves filter / selection / page state.
- Mutations invalidate the relevant TanStack Query keys + the counters.
- Mobile viewport renders without horizontal scroll.
- At least one smoke test per screen.

---

## PR 15 — Decommission

**Goal:** Delete `MarinaDashboardPage.tsx`.

- [ ] Confirm every panel has been migrated.
- [ ] Delete the file. Remove the import from `App.tsx`.
- [ ] Bin any leftover panel components that are no longer referenced.

**Acceptance**

- `git grep MarinaDashboardPage` returns nothing.
- The app still works.

---

## PR 16 — Dashboard widget deep-link tightening

By this point every KPI / inbox row deep-link works (the receiving routes
exist), but you may want a polish pass: hover affordances, "View →" text on
hoverable tiles, the "Filtered via dashboard link" banner on Maintenance.

This is small enough that it can fold into PR 5 (Maintenance) if you don't want
a separate PR.

---

## Optional follow-ups

- **PR α** — `getMarinaComposition(marinaId)` server endpoint for the dashboard
  composition bar, replacing client-side derivation.
- **PR β** — `getBillingSummary(marinaId)` server endpoint for the Billing KPI
  tiles.
- **PR γ** — Drag-and-drop on the Maintenance kanban with `@dnd-kit/sortable`.
  This was deliberately deferred from v1.
- **PR δ** — Slip-map view on Slips screen (post-MVP per the roadmap).
- **PR ε** — Email/SMS announcements as a toggle on the Announcements composer.
