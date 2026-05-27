# Starter code

Drop-in TypeScript snippets sized for the `my-marina` codebase. Not finished
files — each one targets the existing conventions (Tailwind 4 `@theme`, shadcn
primitives, TanStack Query, TanStack Router) and may need a small adjustment
when you paste it in.

| File | Drop-in location | Notes |
|---|---|---|
| `useUrlState.ts` | `src/MyMarina.Web/src/hooks/useUrlState.ts` | TanStack Router-based. Also exports `usePaginationState`. |
| `Pagination.tsx` | `src/MyMarina.Web/src/components/ui/pagination.tsx` | New shadcn-style primitive. |
| `KPI.tsx` | `src/MyMarina.Web/src/components/ui/kpi.tsx` | Used on the dashboard. |
| `badge-extension.tsx` | replaces `src/MyMarina.Web/src/components/ui/badge.tsx` | Semantic variants + `dot` prop. |
| `MarinaWorkspaceLayout.tsx` | `src/MyMarina.Web/src/marina-workspace/MarinaWorkspaceLayout.tsx` | Parent route component. |
| `MarinaRail.tsx` | `src/MyMarina.Web/src/marina-workspace/MarinaRail.tsx` | Sidebar / icon rail. |
| `MarinaTabBar.tsx` | `src/MyMarina.Web/src/marina-workspace/MarinaTabBar.tsx` | Mobile bottom tabs. Requires `Sheet` primitive. |
| `PageHeader.tsx` | `src/MyMarina.Web/src/marina-workspace/PageHeader.tsx` | Exports `PageHeader` + `PageBody`. |
| `nav-config.ts` | `src/MyMarina.Web/src/marina-workspace/nav-config.ts` | Single source of truth for nav. |
| `useMarinaCounters.ts` | `src/MyMarina.Web/src/marina-workspace/useMarinaCounters.ts` | TanStack Query for badge counts. |

## Dependencies to check

- **Sheet primitive.** `MarinaTabBar.tsx` imports `@/components/ui/sheet`. The
  codebase has `dialog`, `alert-dialog`, and `dropdown-menu` but not `sheet`
  yet. Add the shadcn Sheet primitive (Radix Dialog wrapped) before merging the
  TabBar.

- **Tailwind 4 container queries.** Both rail and tab bar use
  `@container/workspace` and `@max-[719px]/workspace:` variants. These work
  out-of-the-box in Tailwind 4. If the codebase is still on Tailwind 3.x for any
  reason, swap them for a stylesheet with `container-type: inline-size` +
  hand-written media-replacement rules.

- **TanStack Router `<Link>`.** The `Link` import is `from
  '@tanstack/react-router'`. If you're not yet on TanStack Router after PR 1,
  this won't resolve — finish PR 1 first.

## What's intentionally NOT here

- The route files (`routes/marina/$marinaId/reservations.tsx` etc.). Those are
  per-screen and the specs in `screens-*.md` cover them in detail. Each route
  follows the same pattern: validate search with Zod, read with `useSearch`,
  filter / sort data, render with `PageHeader` + `PageBody`.

- Tests. Write them as part of each PR; the bar is "a smoke test per route".
