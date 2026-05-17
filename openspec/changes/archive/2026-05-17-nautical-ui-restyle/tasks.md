## 1. Design Token Foundation

- [x] 1.1 Add `@custom-variant dark (&:is(.dark *));` to `src/MyMarina.Web/src/index.css`
- [x] 1.2 Replace all light-mode tokens in `index.css` `:root {}` with the nautical OKLCH palette (background, foreground, card, primary, secondary, muted, accent, destructive, border, input, ring — see design.md Decision D1 for exact values)
- [x] 1.3 Replace all dark-mode tokens in `index.css` `.dark {}` block with the nautical dark-mode OKLCH values (deep navy background, lighter ocean blue primary, adjusted muted/border)
- [x] 1.4 Create `src/MyMarina.Web/src/store/themeStore.ts` — Zustand store with `preference: 'light' | 'dark' | 'system'`, getter `resolvedTheme`, persisted to `localStorage` key `mymarina:theme`
- [x] 1.5 Update `src/MyMarina.Web/src/App.tsx` to read `themeStore` on mount, apply/remove `.dark` class on `document.documentElement`, and subscribe to `window.matchMedia('(prefers-color-scheme: dark)')` when preference is `'system'`

## 2. NavBar Brand Refresh and Dark Mode Toggle

- [x] 2.1 Update `NavBar.tsx` brand mark: replace plain "MyMarina" text with `⚓ MyMarina` (anchor + logotype), linking to `/`
- [x] 2.2 Update `NavBar.tsx` active link styling: active links get accent-color bottom border (use `border-b-2 border-accent` or equivalent token-based class), inactive links use `text-muted-foreground`
- [x] 2.3 Add dark mode toggle icon button to `NavBar.tsx` right side — Sun icon for light, Moon icon for dark, Monitor icon for system. On click, cycle `preference` in `themeStore` (light → dark → system → light)
- [x] 2.4 Migrate all `slate-*` classes in `NavBar.tsx` to design tokens (`bg-card`, `border-border`, `text-foreground`, `text-muted-foreground`, etc.)
- [x] 2.5 Migrate `DemoBanner.tsx` from `slate-*` utilities to design tokens

## 3. Login Page Split Layout

- [x] 3.1 Restructure `LoginPage.tsx` outer layout to `grid md:grid-cols-2 min-h-screen`
- [x] 3.2 Add brand panel (left column, hidden on mobile): dark nautical CSS gradient (`from-primary to-accent` diagonal or linear), centered ⚓ character in large white text, "MyMarina" wordmark below, tagline beneath that. No image assets — pure CSS.
- [x] 3.3 Add compact mobile brand header (visible only on mobile, above the form): ⚓ MyMarina + tagline in a small header block
- [x] 3.4 Restyle the existing login form (right column) with design tokens: replace all `slate-*` with token utilities, `bg-background`, `text-foreground`, `bg-card`, `border-border`, `bg-primary` on submit button

## 4. Backend: Marina Rollup DTO and Query Additions

- [x] 4.1 Add `string? PhotoUrl`, `bool HasPumpOut`, `bool HasElectric`, `bool IsAnyCovered` fields to `MarinaRollupResultDto` in `SlipSearchDtos.cs`
- [x] 4.2 Add filter parameters `bool? InstantBookOnly`, `bool? HasPumpOut`, `bool? HasElectric`, `bool? IsAnyCovered` to `MarinaRollupSearchQuery` in `SlipSearchQueries.cs`
- [x] 4.3 In `MarinaRollupSearchQueryHandler`: add `BOOL_OR(s.has_pump_out) AS has_pump_out`, `BOOL_OR(s.electric_amps_available > 0) AS has_electric`, `BOOL_OR(s.is_covered) AS is_any_covered` to the existing GROUP BY SELECT projection
- [x] 4.4 In `MarinaRollupSearchQueryHandler`: wire `InstantBookOnly`, `HasPumpOut`, `HasElectric`, `IsAnyCovered` filter params as EXISTS predicates (NOT in-memory filtering, NOT price aggregation — pure predicate push-down). `PhotoUrl` always maps to `null` in the query result.
- [x] 4.5 Update `MarinaSearchController.cs` to accept and pass through the four new filter query parameters to the query
- [x] 4.6 Run `npm run generate-api` (with the API running) to regenerate `src/MyMarina.Web/src/api/schema.d.ts`. Do NOT manually edit `schema.d.ts`.
- [x] 4.7 Update `src/MyMarina.Web/src/api/api.ts` to expose `instantBookOnly`, `hasPumpOut`, `hasElectric`, `isAnyCovered` parameters in the `searchMarinaRollup` function signature

## 5. Search Page IA Restructure (Step 1 — Marina Rollup)

- [x] 5.1 Restructure `SearchPage.tsx` outer layout: replace the `grid grid-cols-1 lg:grid-cols-2` layout with a full-viewport split — left panel fixed width (~380px) + right map (`flex-1`), height = `calc(100vh - filterBarHeight - filterChipsRowHeight)`. Map stays pinned; left panel scrolls internally.
- [x] 5.2 Add filter chips row between the filter bar and the split panel: four toggle chip buttons — "Instant Book", "Electric", "Pump-out", "Covered". Active state uses `bg-primary text-primary-foreground`, inactive uses `bg-secondary text-secondary-foreground`. Chip state lives in `useState` alongside other search filters.
- [x] 5.3 Wire filter chips to search: include `instantBookOnly`, `hasElectric`, `hasPumpOut`, `isAnyCovered` in every call to `searchMarinaRollup` (pass `true` when chip is active, omit when inactive)
- [x] 5.4 Add "N marina(s) in view" summary line above the marina card list
- [x] 5.5 Restyle marina cards: add a fixed photo slot (left, ~80×80px) — render `<img>` when `photoUrl` is non-null, render CSS gradient placeholder (ocean blue → sea-foam, centered ⚓) when null
- [x] 5.6 Add amenity badge pills to marina cards: render "Instant", "Electric", "Pump-out", "Covered" pills when the corresponding response fields are `true`
- [x] 5.7 Migrate all remaining `slate-*` classes in `SearchPage.tsx` to design tokens
- [x] 5.8 Verify mobile layout: filter bar stacks vertically, chips row wraps, map and list stack (map half-height, list below)

## 6. Marina Slips Search Page Enhancement (Step 2)

- [x] 6.1 Add filter chips row to `MarinaSlipsPage.tsx`: "Electric" and "Water" chips (wired to existing `hasElectric` and `hasWater` params on `GET /marinas/{id}/slips/search`)
- [x] 6.2 After a search completes, derive chip counts from results (e.g. "Electric (3)") and display in chip label when chip is active
- [x] 6.3 Add photo placeholder slot to `SlipRow` component: same gradient placeholder pattern as step 1 cards (⚓ gradient, `object-cover` when real photo URL is available)
- [x] 6.4 Restyle `MarinaSlipsPage.tsx`: marina header bar, back button, slip cards, map panel — all migrated from `slate-*` to design tokens
- [x] 6.5 Add marina blurb panel on the map panel (positioned at map bottom): "About [Marina Name]" label + description text pulled from the marina response

## 7. Home Page Polish

- [x] 7.1 Migrate `HomePage.tsx` from `slate-*` to design tokens throughout (background, card, text, badge, button classes)
- [x] 7.2 Restyle the setup banner (currently `bg-blue-600`): use `bg-primary text-primary-foreground` so it follows the theme token
- [x] 7.3 Restyle the draft marina card border (`border-amber-300 bg-amber-50`): keep amber for draft state but ensure text/button elements use tokens

## 8. Remaining Pages — Token Adoption

- [x] 8.1 Migrate `MarinaDashboardPage.tsx` — replace all `slate-*` with tokens; do not change layout
- [x] 8.2 Migrate `SlipDetailPage.tsx` — replace all `slate-*` with tokens
- [x] 8.3 Migrate `MyBoatsPage.tsx` — replace all `slate-*` with tokens
- [x] 8.4 Migrate `MyTripsPage.tsx` — replace all `slate-*` with tokens
- [x] 8.5 Migrate `MySlipsPage.tsx` — replace all `slate-*` with tokens
- [x] 8.6 Migrate `MyInvoicesPage.tsx` — replace all `slate-*` with tokens
- [x] 8.7 Migrate `MaintenancePage.tsx` — replace all `slate-*` with tokens
- [x] 8.8 Migrate `ProfilePage.tsx` — replace all `slate-*` with tokens
- [x] 8.9 Migrate `PlatformOperatorPage.tsx` — replace all `slate-*` with tokens
- [x] 8.10 Migrate `MarinaSetupWizardPage.tsx` — replace all `slate-*` with tokens
- [x] 8.11 Migrate `MarinaOnboardingPage.tsx`, `PrivateDockOnboardingPage.tsx`, `DockominionOnboardingPage.tsx` — replace all `slate-*` with tokens
- [x] 8.12 Migrate `VesselSelector.tsx`, `VesselDimensionInputs.tsx`, `MapPicker.tsx` shared components — replace all `slate-*` with tokens
