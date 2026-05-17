## Context

The frontend is built on React 19 + Tailwind CSS v4 + shadcn/ui (Radix UI primitives). The shadcn token system is installed in `index.css` but all tokens are zero-chroma OKLCH (pure grays) and all pages bypass the token system entirely, using raw `slate-*` Tailwind utilities. Dark mode support is declared (`.dark {}` block exists) but non-functional because `dark:` variants are never used in page code.

The search surfaces are the highest-traffic user-facing flows: marina rollup (step 1) and slip list at a marina (step 2). The marina rollup backend query (`MarinaRollupSearchQueryHandler`) already does a single grouped pass over slips and availability windows; price filtering is already supported as a query input but price range is intentionally NOT returned in results (performance constraint: aggregating min/max rates across all windows is too expensive at scale).

## Goals / Non-Goals

**Goals:**
- Introduce a cohesive nautical brand identity via the shadcn/Tailwind token system
- Full dark mode support (system-preference + manual toggle) that works automatically once tokens are adopted
- Migrate all pages from raw `slate-*` to design tokens — single future theme changes flow from `index.css` only
- Restructure the search step-1 layout to match the wireframe IA (left list panel + right map, full viewport height, filter chips, richer cards)
- Add performant amenity filter chips to both search steps, wired to the API
- Ship photo-ready marina cards (placeholder now, real photo URL when upload feature lands)
- Responsive login split layout

**Non-Goals:**
- Price range display on marina rollup cards (performance — never aggregate rates across windows in the rollup query)
- Marina photo upload / management (future feature — DTO has `photoUrl` but it stays `null` until that feature ships)
- Per-slip map visualization on the step-2 page (deferred — single marina pin only)
- Marketing site restyling (separate codebase, separate change)
- Any database migrations (all changes are application-layer)

## Decisions

### D1: Token palette — OKLCH with blue-tinted neutrals throughout

**Decision**: Replace all zero-chroma tokens with OKLCH values that carry a subtle blue-nautical undertone even in neutrals. Specific values:

```css
/* Light mode */
--background:         oklch(99% 0.004 230);   /* near-white, faint sea tint */
--foreground:         oklch(18% 0.06 245);    /* deep navy text */
--card:               oklch(100% 0 0);         /* pure white card surfaces */
--card-foreground:    oklch(18% 0.06 245);
--primary:            oklch(52% 0.18 235);    /* ocean blue — main CTA color */
--primary-foreground: oklch(99% 0.004 230);
--secondary:          oklch(94% 0.04 230);    /* pale blue-gray */
--secondary-foreground: oklch(28% 0.08 240);
--muted:              oklch(96% 0.02 230);    /* subtle background tint */
--muted-foreground:   oklch(52% 0.05 235);    /* secondary text */
--accent:             oklch(80% 0.10 185);    /* sea-foam teal — highlights, active states */
--accent-foreground:  oklch(20% 0.06 220);
--destructive:        oklch(58% 0.22 25);     /* red — unchanged */
--destructive-foreground: oklch(99% 0 0);
--border:             oklch(88% 0.04 230);    /* blue-tinted border */
--input:              oklch(88% 0.04 230);
--ring:               oklch(52% 0.18 235);    /* focus ring = primary */
--radius:             0.625rem;               /* unchanged */

/* Dark mode (.dark) */
--background:         oklch(16% 0.06 245);    /* deep navy */
--foreground:         oklch(94% 0.01 230);    /* near-white */
--card:               oklch(21% 0.07 245);    /* slightly lighter surface */
--card-foreground:    oklch(94% 0.01 230);
--primary:            oklch(65% 0.18 225);    /* lighter blue — readable on dark */
--primary-foreground: oklch(16% 0.06 245);
--secondary:          oklch(27% 0.07 245);
--secondary-foreground: oklch(90% 0.02 230);
--muted:              oklch(24% 0.06 245);
--muted-foreground:   oklch(65% 0.04 230);
--accent:             oklch(35% 0.09 200);    /* dark teal surface */
--accent-foreground:  oklch(85% 0.08 185);
--destructive:        oklch(65% 0.20 25);
--destructive-foreground: oklch(99% 0 0);
--border:             oklch(30% 0.07 245);
--input:              oklch(30% 0.07 245);
--ring:               oklch(65% 0.18 225);
```

**Rationale**: OKLCH gives perceptually uniform color — the blue chroma in muted/border tokens gives the UI a nautical "feel" even in backgrounds, without being loud. Zero-chroma alternatives (pure grays) feel sterile. OKLCH is already the project's chosen color space.

**Alternative considered**: HSL palette. Rejected — OKLCH is already in use and gives better perceptual uniformity, especially for dark mode where HSL values feel inconsistent at low lightness.

---

### D2: Dark mode — class-based `.dark` + Zustand store

**Decision**: Apply `.dark` class to `<html>` element. On mount, `App.tsx` reads from `themeStore` (Zustand, persisted via `localStorage`). Store has three values: `'light'`, `'dark'`, `'system'`. When `'system'`, check `window.matchMedia('(prefers-color-scheme: dark)')`. NavBar shows a Sun/Moon icon toggle that cycles between light → dark → system.

Tailwind v4 requires an explicit custom variant in `index.css`:
```css
@custom-variant dark (&:is(.dark *));
```

**Rationale**: Class-based gives manual override capability. The system-preference fallback means it works correctly for users who never touch the toggle. Zustand is already the app's state management library.

**Alternative considered**: Pure CSS `@media (prefers-color-scheme: dark)` only. Rejected — user said they want a manual toggle, which requires JS-driven class application.

---

### D3: Token migration strategy — page-by-page, no compatibility shim

**Decision**: Replace `slate-*` utilities with token-based utilities (`bg-card`, `text-foreground`, `text-muted-foreground`, `border-border`, etc.) directly in each page/component file. No intermediate compatibility layer. Since the app is not in production, there is no risk of breaking deployed users.

The migration priority order:
1. `index.css` + `themeStore` + `App.tsx` (foundation — dark mode works after this)
2. `NavBar`, `DemoBanner` (global shell — visible on every page)
3. `LoginPage` (entry point — first brand impression)
4. `SearchPage`, `MarinaSlipsPage` (highest-traffic surfaces, also get IA changes)
5. `HomePage` (dashboard entry)
6. All remaining pages (mechanical token swap)

**Rationale**: No shim means no tech debt. Pages not yet migrated still render correctly (they just don't respond to dark mode). The priority order ensures the most visible surfaces are done first.

---

### D4: Search layout — left panel fixed, right map fills remainder

**Decision**: The search step-1 page becomes a full-viewport two-column layout:
```
┌─ filter bar (full width) ──────────────────────────────────┐
├─ filter chips row ─────────────────────────────────────────┤
├─ left panel (380px fixed) ──┬─ map (flex-1, full height) ──┤
│ summary line                │                              │
│ marina card list            │  [Leaflet map]               │
│ (overflow-y scroll)         │  "Search this area" button   │
└─────────────────────────────┴──────────────────────────────┘
```
Total height = `calc(100vh - filterBar - filterChips)`. Mobile: stacks vertically (filter bar → chips → map half-height → list).

**Rationale**: Matches the wireframe IA. Full-height fixed layout prevents the map from being pushed off-screen when many marina cards appear. This matches the Zillow/Airbnb pattern users expect for map-based search.

---

### D5: Filter chips — EXISTS predicates in query handler, BOOL_OR in GROUP BY

**Decision**: Four filter chips: Instant Book, Electric, Pump-out, Covered.

**Backend implementation (performance-safe)**:

Filter parameters become `WHERE` conditions added to the existing query before aggregation:
```sql
-- Instant Book: already supported
-- Electric (hasPumpOut, isAnyCovered): added as subquery predicates
AND (
  @hasElectric IS NULL OR EXISTS (
    SELECT 1 FROM slips s2
    WHERE s2.marina_id = m.id
      AND s2.electric_amps_available > 0
      AND s2.id = s.id  -- correlated to already-filtered slip set
  )
)
```

Response badge fields added to the `SELECT` in the existing GROUP BY:
```sql
BOOL_OR(s.has_pump_out)              AS has_pump_out,
BOOL_OR(s.electric_amps_available > 0) AS has_electric,
BOOL_OR(s.is_covered)               AS is_any_covered
```

`BOOL_OR` is a standard aggregate — it rides the existing `GROUP BY marina_id` with no extra pass. Indexed on `slips(marina_id, has_pump_out)` etc. if needed.

**What is explicitly NOT added**: Any join to `availability_windows` for price aggregation. `photoUrl` returns `null` always until the photo upload feature ships.

**Frontend**: Filter chips render as toggle pills. Active chips are sent as query params on every search (initial load + "Search this area" + form submit). State lives in the same `useState` block as other search filters.

---

### D6: Photo placeholder — gradient div, DTO field ready for real URL

**Decision**: `MarinaRollupResultDto` gains `string? PhotoUrl`. When `null` (always, until photo feature ships), the UI renders a CSS gradient placeholder div in the card's image slot:
```
ocean blue → sea-foam gradient + centered ⚓ character in muted color
```
When non-null, renders `<img src={photoUrl} className="object-cover" />`. Same slot, same dimensions. No conditional logic change needed when real photos arrive — just populate the field.

---

### D7: Login split layout — brand panel is a presentational gradient, no image assets needed

**Decision**: Desktop layout is `grid-cols-[1fr_1fr]` at `md` breakpoint. Left panel: dark navy → ocean blue diagonal gradient, white ⚓ at center, tagline below, small "MyMarina" wordmark. Right panel: form (existing login form logic, restyled with tokens). Mobile: only the right panel (form) renders, with a compact brand header (logo + tagline above the form).

No image assets needed — pure CSS gradient. Fast to load, no CDN dependency.

## Risks / Trade-offs

**[Risk] Token migration is mechanical but large** → Mitigation: Handled systematically page-by-page. Each page is self-contained. A missed `slate-*` class is cosmetic only — colors slightly off, not broken. Easy to catch in visual review.

**[Risk] Tailwind v4 `@custom-variant dark` interacts unexpectedly with shadcn components** → Mitigation: shadcn components already use `dark:` variants internally. The custom variant just needs to be declared once in `index.css`. Test NavBar dark/light toggle early (Task 1) to catch any variant resolution issues before all pages are migrated.

**[Risk] BOOL_OR aggregate fields add slight overhead to rollup query** → Mitigation: These are boolean aggregates over the same rows already being aggregated for `COUNT(*)`. PostgreSQL evaluates them in the same pass. At scale, add a composite index on `(marina_id, has_pump_out)`, `(marina_id, is_covered)`, `(marina_id, electric_amps_available)` — standard index additions, no schema migration needed beyond `CREATE INDEX CONCURRENTLY`.

**[Risk] Perceptual color choices look wrong at runtime** → Mitigation: OKLCH values specified precisely. First task is token system + NavBar, which gives immediate visual feedback before touching complex pages.

**[Trade-off] No price range on marina cards** → Intentional. The performance constraint is preserved. Users can filter by price range (existing capability) but the cards don't display a price range. This is acceptable at MVP; a dedicated caching layer or pre-computed column can unlock it later.

## Migration Plan

No production users — no rollback plan needed. All changes are additive (new DTO fields are optional, new filter params are optional/nullable). The API remains fully backward-compatible.

## Open Questions

None — all decisions made during exploration session with the team.
