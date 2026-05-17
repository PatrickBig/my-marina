## Why

The current MyMarina UI is visually undifferentiated — an achromatic slate-gray palette with no brand identity, raw Tailwind utilities bypassing the design token system, and no dark mode support. As the product moves toward public launch, first impressions matter: the login page, slip search, and home screen need to communicate a trustworthy, maritime brand rather than an unstyled scaffold.

## What Changes

- **Nautical design token system**: Replace all zero-chroma OKLCH tokens in `index.css` with a maritime palette (ocean blues, sea-foam teal, deep navy, blue-tinted neutrals). Full dark mode token set added alongside light mode.
- **Dark mode support**: Class-based `.dark` strategy with Tailwind v4 `@custom-variant dark`. Zustand `themeStore` persists user preference (light/dark/system) to `localStorage`; `App.tsx` initializes on mount from stored preference or `prefers-color-scheme`.
- **Dark mode toggle in NavBar**: Sun/moon icon button in the navigation bar.
- **NavBar brand refresh**: Anchor mark (⚓) + "MyMarina" logotype, active-link underline in accent color, responsive layout.
- **Login page split layout**: Desktop — nautical gradient brand panel (left) + form (right). Mobile — centered form with logo only.
- **Search page IA restructure (step 1 — marina rollup)**: Left panel (marina list, fixed height) + right map, filter chips row (Instant Book / Electric / Pump-out / Covered) wired to the API, "N marinas · M slips fit" summary line, richer marina cards (photo placeholder, amenity badge pills).
- **Search page enhancement (step 2 — slips at marina)**: Filter chips, richer slip cards with amenity badges, marina blurb panel on map.
- **Marina rollup API additions**: `photoUrl`, `hasPumpOut`, `hasElectric`, `isAnyCovered` fields on `MarinaRollupResultDto`; corresponding filter query params on `MarinaRollupSearchQuery`. Implemented as `BOOL_OR` aggregates (free in existing `GROUP BY` pass) and `EXISTS` predicates — no price aggregation, no extra query passes.
- **Token adoption across all pages**: Every page migrated from raw `slate-*` utilities to shadcn tokens (`bg-primary`, `text-muted-foreground`, `bg-card`, etc.), making dark mode automatic.

## Capabilities

### New Capabilities

- `nautical-design-system`: Nautical color token system with full light/dark mode support, maritime palette, and Tailwind v4 dark variant wiring.
- `search-filter-chips`: Amenity filter chips on marina rollup search (Instant Book, Electric, Pump-out, Covered) wired to the API as boolean query parameters, with corresponding response fields for rendering badges on marina cards.

### Modified Capabilities

- `slip-search`: Marina rollup result shape gains `photoUrl`, `hasPumpOut`, `hasElectric`, `isAnyCovered` response fields and four new optional filter parameters (`instantBookOnly`, `hasPumpOut`, `hasElectric`, `isAnyCovered`).

## Impact

**Backend**
- `MyMarina.Application/Search/SlipSearchDtos.cs` — `MarinaRollupResultDto`, `MarinaRollupSearchQuery`
- `MyMarina.Infrastructure/Search/MarinaRollupSearchQueryHandler.cs` — query handler (aggregate + filter additions)
- `MyMarina.Api/Controllers/MarinaSearchController.cs` — query param binding for new filters
- API schema regeneration required (`npm run generate-api`) after backend changes land

**Frontend**
- `src/MyMarina.Web/src/index.css` — token palette replacement
- `src/MyMarina.Web/src/store/themeStore.ts` — new file
- `src/MyMarina.Web/src/App.tsx` — dark mode init
- `src/MyMarina.Web/src/components/NavBar.tsx` — brand + toggle
- `src/MyMarina.Web/src/pages/LoginPage.tsx` — split layout
- `src/MyMarina.Web/src/pages/SearchPage.tsx` — IA restructure
- `src/MyMarina.Web/src/pages/MarinaSlipsPage.tsx` — IA enhancement
- `src/MyMarina.Web/src/pages/HomePage.tsx` — token adoption + polish
- All remaining pages — token adoption only
- `src/MyMarina.Web/src/api/schema.d.ts` — regenerated (never hand-edited)
- `src/MyMarina.Web/src/api/api.ts` — updated to expose new filter params

**No database migrations required** — all changes are application-layer only.
