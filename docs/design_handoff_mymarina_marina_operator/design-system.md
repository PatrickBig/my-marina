# Design system additions

## What's not changing

**Tokens stay.** Don't touch `src/MyMarina.Web/src/index.css`. Every value used in
the prototype already maps to an existing CSS custom property (`--card`,
`--primary`, `--accent`, `--muted-foreground`, etc.).

**The shadcn primitives stay.** `Button`, `Input`, `Select`, `Dialog`,
`AlertDialog`, `Dropdown`, `Card`, `Table`, `Separator`, `Label` are all kept.

## What's being added

Three categories, all small and additive.

### 1. Badge — semantic variants

The current `badge.tsx` exports one default variant. Extend it with semantic
variants used throughout the prototype:

```ts
type BadgeVariant =
  | 'neutral'      // default — no status meaning
  | 'primary'      // ocean blue tint
  | 'accent'       // sea-foam tint
  | 'success'      // green
  | 'warning'      // amber
  | 'destructive'; // red
```

Plus a `dot` prop that prepends a 6 px filled circle.

The recipe uses `color-mix(in oklch, …)` so each variant works in both light and
dark mode without per-mode rules. See
[`starter-code/badge-extension.tsx`](./starter-code/badge-extension.tsx).

### 2. Two new components

| Component | Purpose | File |
|---|---|---|
| `<KPI>` | Dashboard KPI tile (label + value + hint + optional delta + optional click target). | `components/ui/kpi.tsx` |
| `<Pagination>` | Page-number bar + prev/next + range readout. Replaces "Load all" links. | `components/ui/pagination.tsx` |

Both are tiny — under 150 LOC each. See `starter-code/`.

### 3. Two new hooks

| Hook | Purpose | File |
|---|---|---|
| `useUrlState(key, default)` | Get/set one query-string param with default fallback. | `hooks/useUrlState.ts` |
| `useMarinaCounters(marinaId)` | TanStack Query for sidebar badge counts. | `marina-workspace/useMarinaCounters.ts` |

## Patterns to reuse, not invent

### Selectable surface

Slips dock rail cards, reservation cards, and pricing-plan cards all share this
hover + selected style. Bake it as a utility class:

```css
@utility selectable-card {
  cursor: pointer;
  transition: border-color 0.12s, box-shadow 0.12s;
}
@utility selectable-card-hover {
  border-color: color-mix(in oklch, var(--primary) 30%, var(--border));
}
@utility selectable-card-selected {
  border-color: var(--primary);
  box-shadow: 0 0 0 1px var(--primary) inset;
  background: color-mix(in oklch, var(--primary) 6%, var(--card));
}
```

Or, more Tailwind-idiomatic, a `data-state="selected"` attribute on the card with
matching `data-[state=selected]:` variants. Either is fine — pick one and use it
everywhere.

### Filter chip row

Used on Slips, Customers, Assignments, Reservations, Invoices, Maintenance. Same
shape every time:

```tsx
<div className="flex flex-wrap items-center gap-2 mb-3">
  <Input
    icon={<Search className="size-4" />}
    placeholder="Search…"
    className="flex-1 min-w-[280px]"
  />
  {chips.map(({ key, label, count }) => (
    <FilterChip
      key={key}
      count={count}
      active={status === key}
      onClick={() => setStatus(key)}
    >
      {label}
    </FilterChip>
  ))}
</div>
```

`FilterChip` is just a styled `Button` variant. Active state uses `--primary`
foreground.

### Right-side drawer

Customers, Reservations, and (optionally) Maintenance use a right-side detail
panel. At ≥ 1100 px it's a 340–360 px column next to the list; below that, it
becomes a full-width sheet that pushes the list down (or, better, a Radix
`<Sheet>` that slides in over the list — your call).

Selection state is in the URL (`?id=…`), so the drawer survives reload.

### Mobile-stacked table

Tables on Slips, Customers, Assignments, and Invoices convert to stacked card
layouts at < 640 px via CSS Grid on the row. The recipe:

```css
@container workspace (max-width: 639px) {
  .tbl-stack-sm thead { display: none; }
  .tbl-stack-sm tbody tr {
    display: grid;
    grid-template-columns: auto 1fr auto;
    gap: 4px 10px;
    padding: 12px;
  }
  .tbl-stack-sm tbody td { padding: 0; border: 0; }
  /* explicit row/column placement per cell — varies per table */
}
```

This is preferable to a heavy `react-table` rewrite. The current tables are
simple enough that hand-crafting the responsive form is faster and more readable.

## Iconography

Use Lucide (already a dep). Resist the urge to draw custom SVGs for icons —
Lucide's set covers everything in the prototype:

| Concept | Lucide icon |
|---|---|
| Dashboard | `LayoutGrid` |
| Reservations / Calendar check | `CalendarCheck` |
| Slips & anchor | `Anchor` |
| Listings calendar | `Calendar` |
| Customers / Users | `Users` |
| Assignments / Leases | `ListChecks` |
| Billing / Invoice | `Receipt` |
| Maintenance / Wrench | `Wrench` |
| Announcements | `Megaphone` |
| Staff / Shield | `Shield` |
| Settings | `Settings` |
| Pricing | `DollarSign` |

Stick to `size={16}` (or Tailwind `size-4`) for inline icons. `size-5` for
section headers. Never bigger than 20 px inside data rows.

## Don't

- Don't introduce a new chart library. The dashboard occupancy ring is 30 lines
  of inline SVG (see `screens-operations.md`).
- Don't write `as any`-shaped Zod resolvers. The existing forms do this because
  of a Zod-v4 + react-hook-form quirk that has since been resolved upstream — if
  you bump the resolver package, the cast disappears.
- Don't reach for Zustand for any of this. Auth uses it already; URL state and
  data are query / router.
