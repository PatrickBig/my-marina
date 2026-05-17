# Handoff: MyMarina top bar

## Overview

This handoff specifies the **global top bar** for the MyMarina app — the
navigation chrome that appears on every authenticated boater screen. It
replaces the previous bar (which had a horizontal scrollbar, exposed
"Sign out" as a casual link, and had no clear active state).

The design is **responsive**: above a 760px container width it shows a
flat six-item nav with a labelled avatar; below it the secondary items
collapse into a single "My account ▾" dropdown and the avatar narrows to
a circle. A second tighten point at 500px hides the wordmark and the
notifications icon.

---

## About the design files

The files in this bundle are **design references created in HTML** —
prototypes showing intended look and behavior. They are **not** production
code to copy verbatim.

The task is to **recreate this design in MyMarina's existing codebase**,
using the project's established patterns (component library, theming,
routing, state management). If MyMarina does not yet have a component
library, use the framework's idiomatic primitives — `<DropdownMenu>` from
Radix / Headless UI / shadcn-ui all map cleanly onto the menu pattern
specified here.

The CSS in `top-bar-reference.html` is illustrative — production should
use the project's existing token system rather than the inline `--mm-*`
custom properties.

---

## Fidelity

**High-fidelity.** Final colors, typography, spacing, breakpoints, and
interactions. The bar should be pixel-matched.

---

## Files in this bundle

| File | Purpose |
| --- | --- |
| `README.md` | This document. |
| `top-bar-reference.html` | Drop-in working reference. Open in a browser, resize the window to see the responsive collapse. Single bar markup; vanilla JS for menu behavior. **Read this as the source of truth.** |
| `showcase-all-options.html` | The full exploration deck with five options (A–E) and the recap table. Useful for context on why this design was chosen; not required reading. |

---

## The bar at a glance

```
┌─────────────────────────────────────────────────────────────────────────┐
│ ⚓ MyMarina   [Find a slip]  My trips  My slips  My boats  …  🔔  PB ▾  │   ≥ 760px (Option A)
└─────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────┐
│ ⚓ MyMarina  [Find a slip]  My account ▾  🔔 PB ▾ │   < 760px
└─────────────────────────────────────────────┘

┌─────────────────────────────┐
│ ⚓  [Find a slip]  My account ▾  PB ▾ │   < 500px
└─────────────────────────────┘
```

Brackets denote the active-page pill. Find a slip is the only nav item
that never collapses.

---

## Layout

### The shell (`.mm-topbar-shell`)

- Outer container that establishes the **container-query scope**.
- `container-type: inline-size; container-name: mm-topbar;`
- Background: `--mm-bar-bg`.
- Sticky or fixed positioning is left to the page layout — the component
  doesn't take an opinion.

### The bar (`.mm-topbar`)

| Property | Value |
| --- | --- |
| Height | **56px** |
| Padding | `0 22px` (`0 14px` below 360px container) |
| Display | `flex` |
| Align items | `center` |
| Gap | none — children spaced with `margin-right` / `gap` on inner groups |
| Border-bottom | `1px solid rgba(255,255,255,0.04)` |
| Color | `--mm-ink` (`#ffffff`) |
| Font | Inter, 14px, 500 weight default |

### Bar structure (left → right)

1. **Brand** — anchor / logo + wordmark. Links to `/` (boater dashboard).
2. **Primary nav** — list of links.
3. **Spacer** — `flex: 1` pushes the right cluster.
4. **Right cluster** — notifications icon, avatar menu trigger.

### Dropdown menus

Two menus live as siblings of `.mm-topbar` inside the shell, absolutely
positioned beneath the bar:

| Menu | Trigger | Alignment | Min width |
| --- | --- | --- | --- |
| Avatar / account preferences | The avatar pill | Right (`right: 22px`) | 232px |
| "My account" (collapsed nav) | The "My account ▾" button | Left (`left: 22px`) | 232px |

Both share the same `.mm-menu` styles. Open state is toggled via
`data-open="true"`.

---

## Design tokens

All values are in `top-bar-reference.html` as CSS custom properties.
Map these onto your existing token system rather than copying the
custom properties verbatim.

### Color

| Token | Value | Where it's used |
| --- | --- | --- |
| `--mm-bar-bg` | `#0e1c2e` | Bar background |
| `--mm-menu-bg` | `#16263c` | Dropdown background |
| `--mm-menu-border` | `rgba(255,255,255,0.08)` | Dropdown border |
| `--mm-divider` | `rgba(255,255,255,0.06)` | Menu separators |
| `--mm-ink` | `#ffffff` | Primary text |
| `--mm-ink-muted` | `#c6d2e0` | Inactive nav items, secondary text |
| `--mm-ink-faint` | `#94a3b8` | Meta text, icons inside menus |
| `--mm-accent` | `#5ab7ff` | Active-page text, focus rings, active dot |
| `--mm-accent-soft` | `rgba(90,183,255,0.16)` | Active-page pill background |
| `--mm-danger` | `#ef6b6b` | "Sign out", overdue invoice badge |
| `--mm-unread` | `#ff7a5c` | Notifications dot |

Avatar gradient: `linear-gradient(135deg, #5ab7ff 0%, #2c6cb0 100%)`,
text color `#062035`.

### Geometry

| Token | Value |
| --- | --- |
| `--mm-bar-h` | `56px` |
| `--mm-radius` | `6px` (nav item, icon button) |
| `--mm-radius-lg` | `10px` (dropdown menu) |
| Avatar size | `26 × 26px`, fully rounded |
| Icon button | `34 × 34px`, 8px radius |

### Type

| Element | Family | Size | Weight |
| --- | --- | --- | --- |
| Brand wordmark | Inter | 14px | 700 |
| Nav item | Inter | 14px | 500 (600 on active) |
| Avatar name | Inter | 13px | 500 |
| Menu item | Inter | 13.5px | 400 |
| Menu section label | Inter | 10.5px | 600, uppercase, letter-spacing 0.08em |
| Menu meta / counts | Inter | 11px | 400, `--mm-ink-faint` |

### Spacing

- Brand → nav: `margin-right: 28px` on `.mm-brand`
- Nav item padding: `7px 12px`
- Icon button: `width/height: 34px`
- Avatar button padding: `4px 10px 4px 4px`
- Cluster gap (notifications ↔ avatar): `6px`

---

## Components

### Brand (`.mm-brand`)

- Element: `<a href="/">`
- Aria-label: `"MyMarina, home"`
- Children: 16px anchor SVG (`currentColor`) + wordmark span
- Wordmark hides below 500px container width via `.mm-wordmark { display: none }`
- Focus: 2px `--mm-accent` outline, 3px offset

### Nav (`.mm-nav`)

- Element: `<nav aria-label="Primary">`
- Children are anchors. They are NOT in a `<ul>` here for simplicity, but
  use whatever your component lib expects.
- Each item: `padding: 7px 12px; border-radius: 6px;`
- **Active state** uses `aria-current="page"`:
  - Color: `--mm-accent`
  - Background: `--mm-accent-soft`
- Hover: background `rgba(255,255,255,0.06)`, color white
- Focus-visible: 2px `--mm-accent` outline

#### Find a slip

- **Always visible**, regardless of container width.
- Renders as the first nav item.

#### Collapsible items (`.mm-collapsible`)

Six links in this order:

1. My trips → `/trips`
2. My slips → `/slips`
3. My boats → `/boats`
4. Maintenance → `/maintenance`
5. Invoices → `/invoices`

Below the 760px breakpoint they all hide. **Order is identical inside
the "My account" dropdown** so muscle memory survives the collapse.

#### My account button (`.mm-account-btn`)

- Only visible below 760px.
- `<button aria-haspopup="menu" aria-expanded aria-controls="mm-account-menu">`
- Label: "My account" + chevron
- **Active indicator**: when the current page is one of the collapsed
  items, set `data-active="true"` on this button — adds a small accent dot
  after the chevron so the user can see something inside is current.

### Notifications button (`.mm-icon-btn.mm-notif-btn`)

- 34×34 square, `currentColor` bell icon at 16px
- Unread indicator: 7×7 dot at top-right (`--mm-unread`) with a 2px
  ring of `--mm-bar-bg` for separation
- Toggled via `data-unread="true"` attribute
- Aria-label: `"Notifications, N unread"` (live region — update count)
- Hidden below 500px (move notifications inside the avatar menu at that
  width if needed)

### Avatar button (`.mm-avatar-btn`)

- `<button aria-haspopup="menu" aria-expanded aria-controls="mm-avatar-menu">`
- Children: 26×26 avatar circle (initials) + name span + chevron
- Border: `1px solid rgba(255,255,255,0.08)`, fully rounded
- Below 760px: name span hides; button shrinks to avatar+chevron only

### Dropdown menu (`.mm-menu`)

- Container background `--mm-menu-bg`, 10px radius, 1px border, drop shadow `0 14px 32px -8px rgba(0,0,0,0.5)`
- Padding 6px (frame), each item 8px 12px with 6px radius
- Items: icons left (14px, `--mm-ink-muted`), label, optional meta on right (`margin-left: auto`)
- Section labels: 10.5px uppercase, `--mm-ink-faint`, padding `8px 12px 4px`
- Separators: 1px line in `--mm-divider`, 4px margin top/bottom
- Header block (avatar menu only): name (13px/600) + email (11.5px, faint)
- Open animation: 120ms ease-out, fade + 4px translate-down

#### Avatar menu items (in order)

```
[header: name + email]
─────
Account & profile        → /account
Payment methods          → /payment-methods
Notifications            → /notifications        (preferences, not the feed)
─────
Dark mode  · toggle      → onClick toggles theme; meta shows "On" / "Off"
Help & support           → /help
─────
Sign out (danger)        → onClick signs out
```

#### Account menu items (collapsed-nav mode)

```
RESERVATIONS
My trips      · 3 upcoming      → /trips
My slips      · 2               → /slips
My boats      · 1               → /boats
─────
BILLING & SERVICE
Invoices      · 1 due (danger)  → /invoices
Maintenance requests            → /maintenance
```

Counts and badges come from the same data that drives the dashboard
cards — they should be live, not static.

---

## Interactions & behavior

### Menu open / close

- Click a trigger → toggles its menu open/closed
- Opening a menu first closes any other open menu
- Outside click closes all menus
- `Escape` closes the open menu **and returns focus to its trigger**
- Opening a menu **moves focus to the first item** for keyboard users

### Keyboard

- Tab order: brand → nav items left-to-right → notifications → avatar
- Inside a menu: ↑/↓ should move focus between items (handled natively
  by `role="menu"` + `role="menuitem"` in some frameworks; in others
  you'll wire arrow-key handling yourself)
- `Enter` / `Space` activates an item

### Focus rings

All interactive elements use `outline: 2px solid var(--mm-accent);
outline-offset: 1–3px;` on `:focus-visible`. Do not strip these for
visual reasons.

### Active page

Set `aria-current="page"` on the active anchor. When a collapsed item
is current, also set `data-active="true"` on the `.mm-account-btn` so
the dot indicator shows.

### Hover transitions

- Nav items: background + color transition, 120ms ease
- Icon buttons: same
- Avatar button: background + border-color transition, 120ms ease
- Menu items: instant background change (no transition needed)

---

## Responsive behavior

The shell sets up a container query named `mm-topbar`. **Use container
queries, not media queries.** This lets the bar embed correctly inside
narrower regions (a settings panel, a print preview).

### Breakpoints

| Width | What changes |
| --- | --- |
| **Default (≥ 760px)** | Full six-item nav. Avatar shows name. Notifications visible. |
| **< 760px** | `.mm-collapsible` items hide. `.mm-account-btn` shows. Avatar name hides. |
| **< 500px** | Brand wordmark hides (anchor icon only). Notifications icon hides. |
| **< 360px** | Bar horizontal padding tightens to 14px. |

### Stable promises

These do **not** change with width:

- "Find a slip" is always the first primary item
- The avatar menu is always available (theme, sign out)
- Item order inside the "My account" menu matches their order in the wide-mode nav

---

## State management

### Inputs the bar needs

```ts
type TopBarProps = {
  currentUser: {
    name: string;          // "Patrick Bigler"
    email: string;         // "patrick@bigler.io"
    initials: string;      // "PB" (max 2 chars, derive if not provided)
  };
  currentPath: string;     // "/find-a-slip" — used to set aria-current
  counts: {
    upcomingTrips: number; // shown in account menu meta
    slips: number;
    boats: number;
    invoicesDue: number;   // shows in danger color if > 0
    unreadNotifications: number;
  };
  theme: 'light' | 'dark';
  onToggleTheme: () => void;
  onSignOut: () => void;
};
```

### Internal state

- `openMenu: 'avatar' | 'account' | null` — which menu is open
- Outside-click and `Escape` handlers manage this

---

## Accessibility checklist

- [x] `<header role="banner">` wraps the bar
- [x] `<nav aria-label="Primary">` on the primary nav
- [x] Brand has `aria-label="MyMarina, home"`
- [x] Active page uses `aria-current="page"`
- [x] Menu triggers have `aria-haspopup="menu"`, `aria-expanded`, `aria-controls`
- [x] Menus have `role="menu"` and an `aria-label`
- [x] Menu items have `role="menuitem"`
- [x] Decorative SVGs have `aria-hidden="true"`
- [x] Notifications button has live label `"Notifications, N unread"`
- [x] All interactive elements have visible `:focus-visible` styles
- [x] Color contrast: text on `--mm-bar-bg` is `#ffffff` (white) for primary, `#c6d2e0` for muted — both ≥ 7:1
- [x] Active pill (`#5ab7ff` on `rgba(90,183,255,0.16)` over `#0e1c2e`) ≥ 5:1

---

## Assets

No external image assets. All icons are inline SVG drawn at 16×16
(nav, brand, dropdown items) or 11–14px (chevrons, meta). The anchor
icon in the brand is original artwork drawn for the brand mark — keep
the exact path data from `top-bar-reference.html`.

If your icon system is Heroicons / Lucide / Feather, you can substitute
the dropdown icons with the equivalent (compass, map-pin, ship, file-text,
wrench, log-out, etc.) — match stroke weight at 1.5px for outlined,
otherwise solid `currentColor`.

---

## Implementation notes

- **Container queries** require browser support (Chrome 105+, Safari 16+,
  Firefox 110+). If you need to support older browsers, fall back to
  media queries on `window.innerWidth` — the breakpoints are identical
  if the bar is full-page-width.
- The animation on menu open is 120ms — short enough to feel
  instantaneous but soft enough to read as a popover, not a flash.
- The bar is intentionally **stateless** for the menu open state in the
  reference — wire it into your framework's state primitive (useState,
  signal, store).
- The "Profile" item from the old design is **gone**. Profile editing
  lives behind the avatar menu as "Account & profile".

---

## How each original issue is resolved

| Original problem | Resolution |
| --- | --- |
| Horizontal scrollbar in nav | Items fit at desktop widths; collapse cleanly below 760px container |
| "Sign out" as a top-level link | Lives inside the avatar menu, marked as a danger item |
| "Profile" both a nav item and a name | Replaced by the avatar menu — no more redundancy |
| Theme toggle as a bare icon | Lives inside the avatar menu as a labelled preference |
| Active state easy to miss | Accent pill (`--mm-accent` color + `--mm-accent-soft` background) |
| Avatar/name disconnect from sign-out flow | Avatar is now the menu trigger; clicking it reveals account actions |

---

## Open questions for the team

1. **"Notifications" as a feed vs preferences.** The avatar menu has a
   "Notifications" item that I've spec'd as preferences — should the
   notifications bell ALSO open a flyout feed of recent notifications,
   or always navigate to `/notifications`? Recommend the former
   (flyout) for parity with most modern apps.
2. **Active dot on the account button.** Currently a single dot. Could
   instead show the active page label inline (e.g., "My account ·
   trips"). Decide based on space and tone.
3. **Boater + Host unified dashboard.** When a user is both a boater
   AND a marina host, do we add a tenant switcher to the bar (left of
   the brand or as a chip in the right cluster)? Out of scope here.
