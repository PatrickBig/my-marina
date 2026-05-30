## ADDED Requirements

### Requirement: TanStack Router replaces hand-rolled path ladder
The application SHALL use TanStack Router (code-based) for all routing. `App.tsx`'s `renderPage()` function and `window.location.pathname` comparisons SHALL be removed and replaced with `<RouterProvider router={router} />`. All routes SHALL be defined in `src/MyMarina.Web/src/router.tsx`. The `__root__` route SHALL render `<DemoBanner />` and the `<NavBar />` above an `<Outlet />`.

#### Scenario: Existing routes continue to work after migration
- **WHEN** a user navigates to any route that existed before this change (e.g., `/search`, `/boats`, `/admin`, `/marina/:id/setup`)
- **THEN** the correct page component renders without error

#### Scenario: Browser back/forward navigation works
- **WHEN** a user navigates between routes and uses the browser back button
- **THEN** the previous route renders correctly with the URL that was active before the forward navigation

### Requirement: Typed Zod search params on every operator route
Every route under `/marina/:marinaId/` SHALL declare a `validateSearch` Zod schema. Default values SHALL be declared in the schema. Invalid param values SHALL be coerced to defaults, never cause an error.

#### Scenario: Unknown search param is ignored
- **WHEN** a user lands on `/marina/:id/reservations?foo=bar`
- **THEN** the page renders using defaults; `foo` is not passed to any component

#### Scenario: Invalid enum value is coerced to default
- **WHEN** a user lands on `/marina/:id/reservations?status=bogus`
- **THEN** `status` is coerced to `"pending"` (the schema default) without throwing

### Requirement: URL carries all filter, selection, and pagination state
The following canonical parameter names SHALL be used consistently across all operator screens. No screen SHALL use sessionStorage, Zustand, or component-local state for values that affect what data is shown in a table or card list.

| Param | Meaning |
|---|---|
| `status` | Active filter chip / tab |
| `id` | Selected row id (drawer open) |
| `page` | 1-indexed page number (omit when 1) |
| `view` | Visual mode toggle (e.g., `board` vs `list`) |
| `col` | Kanban column filter |
| `done` | Completed-items time range |
| `tab` | Sub-tab within a screen |
| `q` | Free-text search query |
| `plan` | Pricing plan id filter |

#### Scenario: Page reload restores UI state
- **WHEN** an operator is on `/marina/:id/reservations?status=confirmed&page=2` and reloads the page
- **THEN** the Confirmed filter chip is active and page 2 of results is shown

#### Scenario: Page filter resets on status change
- **WHEN** an operator changes the status filter on any list screen
- **THEN** `page` is reset to 1 (or dropped from the URL)

### Requirement: `useUrlState` and `usePaginationState` hooks
The `useUrlState(key, default)` hook SHALL be available in `src/MyMarina.Web/src/hooks/useUrlState.ts`. It SHALL read the named search param from the current route and return `[value, setter]`. Calling the setter SHALL update the URL via `navigate` (not `replace`) so the browser history entry is created. Calling setter with the default value SHALL drop the param from the URL.

`usePaginationState()` SHALL wrap `useUrlState('page', '1')` and return `[pageNumber: number, setPage]`.

#### Scenario: Setting a non-default value updates the URL
- **WHEN** `setStatus('confirmed')` is called and the default is `'pending'`
- **THEN** the URL gains `?status=confirmed`

#### Scenario: Setting the default value removes the param
- **WHEN** `setStatus('pending')` is called and the default is `'pending'`
- **THEN** `status` is removed from the URL

### Requirement: All pages updated to use Route.useParams()
No page component SHALL read `window.location.pathname` directly after the router migration. Path params SHALL be accessed via `Route.useParams()` or `useParams({ strict: false })`.

#### Scenario: MarinaDashboardPage marinaId extraction is migrated
- **WHEN** the workspace layout mounts
- **THEN** `marinaId` comes from `useParams()`, not from `getMarinaIdFromPath()`
