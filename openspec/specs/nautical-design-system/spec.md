## Requirements

### Requirement: Nautical design token system

The system SHALL define a maritime color token set in `src/MyMarina.Web/src/index.css` using OKLCH color values. All tokens SHALL carry a subtle blue nautical undertone — including neutrals — so the brand is perceptible even in backgrounds and borders. The token set SHALL include, at minimum: `--background`, `--foreground`, `--card`, `--card-foreground`, `--primary`, `--primary-foreground`, `--secondary`, `--secondary-foreground`, `--muted`, `--muted-foreground`, `--accent`, `--accent-foreground`, `--destructive`, `--destructive-foreground`, `--border`, `--input`, `--ring`, `--radius`.

The `--primary` token SHALL be an ocean blue (`oklch(52% 0.18 235)` in light mode). The `--accent` token SHALL be a sea-foam teal (`oklch(80% 0.10 185)` in light mode). Muted and background tokens SHALL use low-chroma OKLCH values with a blue hue axis (hue ~230–245) rather than zero-chroma grays.

All application pages SHALL use shadcn Tailwind token utilities (`bg-primary`, `text-muted-foreground`, `bg-card`, `border-border`, etc.) rather than raw `slate-*` utilities. Raw `slate-*` utilities SHALL NOT be used in any component or page file after this change is applied.

#### Scenario: Primary button renders in ocean blue

- **WHEN** any page renders a primary action button
- **THEN** the button background uses `--primary` (ocean blue), not slate-gray
- **AND** the button text uses `--primary-foreground`

#### Scenario: Card surfaces use card token

- **WHEN** any card component renders
- **THEN** the background uses `var(--card)` / `bg-card`
- **AND** the text uses `var(--card-foreground)` / `text-card-foreground`

#### Scenario: No raw slate-* classes remain

- **WHEN** the production build is compiled
- **THEN** no component or page file contains bare `slate-` Tailwind utility references

---

### Requirement: Dark mode support

The system SHALL support a dark color theme via the `.dark` class applied to the `<html>` element. The dark mode token set SHALL complement the light mode palette: deep navy backgrounds, lighter ocean blue primary, adjusted muted and border tokens.

The Tailwind v4 dark variant SHALL be declared in `index.css` as `@custom-variant dark (&:is(.dark *))` so that `dark:` utility classes resolve correctly across all components.

#### Scenario: Dark mode tokens are distinct from light mode

- **WHEN** the `.dark` class is applied to `<html>`
- **THEN** `--background` becomes deep navy (lightness ≤ 20%)
- **AND** `--primary` becomes a lighter ocean blue (lightness ≥ 60%) for readability on dark surfaces
- **AND** all shadcn components that use `dark:` variants render correctly

#### Scenario: Tailwind dark: utilities resolve

- **WHEN** a component uses `dark:bg-primary` or `dark:text-muted-foreground`
- **THEN** the utility applies its value when the `.dark` class is present on `<html>`
- **AND** reverts to the light-mode value when `.dark` is absent

---

### Requirement: Theme preference persistence and initialization

The system SHALL maintain a `themeStore` (Zustand) that holds the user's theme preference: `'light'`, `'dark'`, or `'system'`. The store SHALL persist to `localStorage` under the key `mymarina:theme`.

On application mount, `App.tsx` SHALL read the stored preference and apply the `.dark` class to `document.documentElement` accordingly. When the preference is `'system'`, the app SHALL check `window.matchMedia('(prefers-color-scheme: dark)')` and apply `.dark` if the result is dark. The system media query SHALL also be subscribed to so that a system-level theme change updates the UI without a page reload.

#### Scenario: User has no stored preference (first visit)

- **WHEN** a user visits for the first time with no `mymarina:theme` in localStorage
- **THEN** the app applies `'system'` as the default preference
- **AND** dark mode is active if and only if the OS is in dark mode

#### Scenario: Stored preference is 'dark'

- **WHEN** the app loads and localStorage contains `mymarina:theme=dark`
- **THEN** `.dark` is applied to `<html>` immediately before first render (no flash)

#### Scenario: System preference changes while app is open

- **WHEN** the user changes their OS to dark mode while the app is open and preference is 'system'
- **THEN** the app switches to dark mode without a page reload

---

### Requirement: Dark mode toggle in navigation bar

The NavBar SHALL include a toggle control (icon button) that cycles through `'light'` → `'dark'` → `'system'` on each click, updating `themeStore` and applying the `.dark` class immediately. The icon SHALL reflect the current state: Sun icon for light, Moon icon for dark, Monitor icon for system.

#### Scenario: Toggle cycles through three states

- **WHEN** the user clicks the theme toggle while in light mode
- **THEN** the preference becomes 'dark' and the UI immediately applies dark tokens

- **WHEN** the user clicks again
- **THEN** the preference becomes 'system' and the UI matches OS preference

- **WHEN** the user clicks again
- **THEN** the preference returns to 'light'

#### Scenario: Preference persists across page reload

- **WHEN** the user sets the preference to 'dark' and reloads the page
- **THEN** dark mode is active from first render (no flash of light theme)

---

### Requirement: NavBar brand mark and active link indicator

The NavBar SHALL display an anchor character (⚓) and the "MyMarina" logotype as the leftmost element, linking to `/`. Active navigation links SHALL be visually distinguished from inactive links using an underline in the `--accent` color (sea-foam teal) and bolder weight.

#### Scenario: Active link is underlined in accent color

- **WHEN** the user is on the `/search` route
- **THEN** the "Find a slip" nav link renders with an accent-colored underline
- **AND** all other nav links render without underlines

#### Scenario: Brand mark links to home

- **WHEN** the user clicks the ⚓ MyMarina brand mark
- **THEN** the browser navigates to `/`

---

### Requirement: Login page split layout (responsive)

The login page SHALL use a two-column layout on `md` and larger screens:

- **Left panel**: Dark nautical gradient (`--primary` deep to `--accent`), centered ⚓ character, tagline text, "MyMarina" wordmark. Purely presentational — no form elements. Implemented as CSS gradient, no image assets required.
- **Right panel**: Existing login form content, restyled with design tokens.

On screens smaller than `md`, the left panel SHALL be hidden. The right panel SHALL display a compact brand header (⚓ + "MyMarina" + tagline) above the form.

#### Scenario: Desktop shows split layout

- **WHEN** the viewport width is ≥ 768px (md breakpoint)
- **THEN** the login page renders as two equal columns: brand panel left, form right
- **AND** the brand panel shows the nautical gradient with ⚓ and tagline

#### Scenario: Mobile shows form only

- **WHEN** the viewport width is < 768px
- **THEN** the brand panel column is not rendered
- **AND** a compact brand header (⚓ MyMarina + tagline) appears above the form
- **AND** the form is centered on screen
