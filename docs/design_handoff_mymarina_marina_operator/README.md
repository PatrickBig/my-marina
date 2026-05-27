# MyMarina · Marina Operator Workspace — Handoff Package

This package is the design + implementation brief for restructuring the marina operator
surface in [`my-marina`](https://github.com/PatrickBig/my-marina).

It is written to be handed to **Claude Code** (or any developer) inside the repo.
Read in this order:

| # | File | Purpose |
|---|---|---|
| 1 | [`spec.md`](./spec.md) | Problem, target state, scope, decisions. Read this first. |
| 2 | [`routing.md`](./routing.md) | URL-as-state pattern (the single biggest behavioural change). |
| 3 | [`shell.md`](./shell.md) | Workspace shell, grouped nav, responsive contract. |
| 4 | [`design-system.md`](./design-system.md) | Token additions, new primitives, badge extension. |
| 5 | [`screens-operations.md`](./screens-operations.md) | Dashboard, Reservations, Maintenance, Listings. |
| 6 | [`screens-customers-money.md`](./screens-customers-money.md) | Customers, Assignments, Billing. |
| 7 | [`screens-marina-setup.md`](./screens-marina-setup.md) | Slips, Pricing plans, Announcements, Staff, Settings. |
| 8 | [`pr-sequence.md`](./pr-sequence.md) | Suggested PR breakdown with acceptance criteria. |
| 9 | [`open-questions.md`](./open-questions.md) | Decisions for Patrick before/during implementation. |

### Visual reference

The Anthropic project these docs ship with also contains a clickable hi-fi prototype
at `MyMarina Operator.html`. Every screen and interaction described in these docs is
rendered there with the real ocean-blue tokens from `src/MyMarina.Web/src/index.css`.
When you need to know "what should this look like?", open that file in a browser, click
through, and observe — the visual answers are there.

### Starter code

[`starter-code/`](./starter-code/) contains drop-in TypeScript snippets:

- `useUrlState.ts` — URL-as-state hook
- `Pagination.tsx` — pagination component (replaces "Load all")
- `MarinaShell.tsx` — workspace chrome (sidebar + responsive collapse)
- `PageHeader.tsx` + `PageBody.tsx` — consistent screen scaffolding
- `KPI.tsx` — dashboard KPI tile
- `badge-extension.tsx` — semantic badge variants (extends existing `badge.tsx`)
- `useMarinaCounters.ts` — single TanStack Query hook for sidebar unread counts

These are **not** copy-paste-and-ship — they target the conventions of `my-marina`
(Tailwind 4 theme tokens, shadcn primitives, TanStack Query). Use them as the shape
to match.

### Out of scope for this brief

- Server-side API changes. The existing controller surface stays; everything in
  this brief is achievable against today's API.
- Authentication / permissions. Membership + role guards stay where they are.
- The boater-side marketplace and the wizard onboarding flows. Those are separate.
- Stripe Connect / online payment. That's Era 2.

### Definition of done

The 136 KB `MarinaDashboardPage.tsx` is deleted. Every operator surface lives at its
own route, the URL carries filter state, and the page is usable on mobile.
