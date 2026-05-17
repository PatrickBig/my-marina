# Handoff: MyMarina Wireframes — User-First Refactor

## Overview

Low-fidelity wireframes covering MyMarina's full surface area across all four personas (boater, marina operator, private slip owner, platform operator) plus deep coverage of two in-flight reworks:

1. **Marketplace search rework** — marina-first, viewport-bounded discovery (two-step flow per `docs/marketplace.md`)
2. **Marina onboarding wizard** — multi-step setup with draft state, geocoder, dock/slip bulk builder, preview-adjust, and publish gate (per `openspec/changes/marina-onboarding-wizard/`)

Plus cross-cutting screens: sign-in, ghost-vessel claim, listing-source diagram (Owner / Holder / OwnerForHolder).

## About the Design Files

The HTML files in this bundle are **design references**, not production code. They are sketchy/low-fi prototypes showing layout, information architecture, and flow — built on a Figma-style pan/zoom design canvas with hand-drawn-looking borders for the express purpose of *not* implying final visual styling.

The task is to **recreate these layouts and flows in the existing MyMarina codebase** (`PatrickBig/my-marina`) using the established patterns there — React (frontend) and .NET (API) per `docs/tech-stack.md` — and the codebase's existing component library / styling system. Do not copy the HTML.

The wireframes encode product behavior and structure derived from the docs. When the wireframes and the docs/specs disagree, **the docs/specs win** — they are the source of truth.

## Fidelity

**Low-fidelity wireframes.** Use these for:

- Information architecture (what fields/data appear on each screen)
- Layout and grouping (which controls cluster together, which lanes exist)
- Flow and step order (especially the onboarding wizard and two-step search)
- Empty states and entry points (e.g. setup banner, draft card, "Search this area" overlay)
- The annotations (red-marker text on each frame) which point out schema/state implications

**Do not** match colors, typography, spacing, or the sketchy/hand-drawn aesthetic. Style with the codebase's existing design system. The single accent color in the wireframes (navy-teal `oklch(60% 0.13 230)`) is just to highlight the focused element on each frame — not a brand direction.

## Sections & Frames

The wireframes are organized into 6 sections, viewable on a pan/zoom canvas in `MyMarina Wireframes.html`. Open it in a browser and zoom into any frame; click a frame label to open fullscreen.

### 1. Intro / cross-cutting
| Frame | Purpose |
|---|---|
| Sign in / sign up | Single global account · social + email · explains "one account, every marina" |
| Vessel claim | First-sign-in screen showing pending ghost-vessel claims added by marinas |
| Listing-source diagram | The three sources of marketplace availability (Owner / Holder / OwnerForHolder) + reservation status flow |

### 2. Boater · marketplace + relationships
| Frame | Purpose |
|---|---|
| **Search · step 1 · marinas** | Marina rollup view — viewport-bound search, "Search this area" Zillow-pattern button, marina cards with `AvailableCount` / price range / `RateKind` ("from $X" for Mixed) / `InstantBookAvailable` / distance, vessel-fit dropdown, sort by Most options / Closest / Lowest price |
| **Search · step 2 · slips at marina** | `/search/marinas/{id}` — filtered slip list scoped to one marina, "← Back to marinas", single marina pin, marina blurb |
| Slip detail | Photo grid, dimensions, **Core amenities** (HasPumpOut/IsCovered/IsIndoor as first-class) split from **Marina tags** (custom string `Amenities[]` array), booking card with pricing & Era-1 off-platform notice |
| Confirm reservation | Review/submit screen, vessel selector with "FITS" indicator, optional note to host, payment-status indicator (`Era 1 · off-platform`) |
| Multi-marina dashboard | Upcoming reservations across marinas, my long-term slip, slips I host (cross-role · no toggle), outstanding invoices, requests, announcements |
| My slip · I'm away | Lease policy display (sublet toggles + revenue share %), calendar timeline (me / away / booked), earnings cards, "I'll be away" modal with three options (marina lists / I list / block) |

### 3. Commercial marina onboarding · the wizard
| Frame | Purpose |
|---|---|
| Home · banner + draft card | Entry point: dismissible setup banner for zero-marina users; draft marina card variant with "Continue setup" / "Delete draft" / progress bar |
| Step 1 · Profile | Marina name, type radio, contact info, description, timezone. Side panel narrates draft-creation bookkeeping |
| Step 2 · GPS + geocoder | Address fields, "Locate on map" button, geocoder precision banner (✓ FULL MATCH variant shown; spec covers fallback levels), draggable Leaflet pin, lat/lng autosync, annotated Nominatim fallback chain |
| Step 3 · Dock & slip builder | Dock count + naming convention (Lettered / Numbered / Manual) with prefix/suffix; slip count (Same for all / Different per dock) + slip naming (PerDockReset / PerDockGlobal / Sequential / Manual) with separator/start/pad; per-dock defaults tabs with dimensions, slip type, electric (amperage), water, **HasPumpOut**, **IsCovered**, **IsIndoor**, custom `Amenities[]` tags |
| Step 4 · Preview & adjust | Collapsible per-dock table, bulk-edit + inline-edit, OVERRIDE badge marks slips diverged from dock defaults, add/remove slip/dock, annotated batch endpoint `PUT /marinas/{id}/setup/docks` |
| Step 5 · Publish | Summary card, "List on marketplace" toggle (defaults OFF — explicit opt-in), activate button setting `IsSetupComplete = true` |

### 4. Marina operator · the SaaS toolkit
| Frame | Purpose |
|---|---|
| Operator dashboard | KPI cards (occupancy, open invoices, pending reservations, marketplace earnings), today's arrivals, action queue (incl. `PendingHostMarinaApproval` lane), sublet activity |
| Slips & docks | Dock list + slip table with type/dimensions/power/status/assignment, edit per row, annotated "slip-map view → post-MVP" |
| Reservation inbox | Filterable list (Pending / Confirmed / Today / Past), reservation detail with status-flow ribbon, approve/decline actions |
| Billing accounts | Account list with balance/vessels/status, detail panel showing Members, Vessels (canonical + ghost), Open invoices, action buttons |
| Listing calendar editor | Month view with Window 1 (open) / Window 2 (paused) / booked-out days; right panel for pricing + policy + revenue split snapshot |

### 5. Private slip owner · "add my dock"
| Frame | Purpose |
|---|---|
| Onboarding wizard | "Where is your dock?" — private dock vs. dockominium choice, address, naming, behind-the-scenes Tenant/Marina auto-create explainer |
| Single-slip dashboard | Hosted slip card, earnings cards, reservations list, Era-1 reminder |
| Dockominium · host-marina policy | Policy radio (None / NotifyOnly / RequiresApproval), host-marina fee deduction display, cross-role panel showing "you also rent from Big Bay" |

### 6. Platform operator · trust & safety
| Frame | Purpose |
|---|---|
| Tenants | Filter pills (Commercial / YachtClub / PrivateHosts / Demo / Suspended), tenant table with type/tier/marina+slip counts |
| Listing moderation | Reported-listing queue, selected-listing detail with report reasons + listing audit + actions (take down / dismiss / disable host) |
| User detail | Memberships + BillingAccount memberships + Vessels split into three cards, reservation history, cross-tenant audit log |

## Interactions & Behavior

The wireframes are static. Behavior is sourced from `docs/` and `openspec/changes/marina-onboarding-wizard/specs/`. Key flows the wireframes imply:

### Search (two-step)
- **Step 1:** `GET /marinas/search` with bbox = current Leaflet viewport. Default to geolocation; pan/zoom reveals "Search this area" button which re-runs the search against the new viewport. Vessel-fit dropdown defaults to `localStorage.mymarina:lastSelectedVesselId`, falling back to most recently created vessel.
- **Step 2:** Click a marina row → navigate to `/search/marinas/{marinaId}` with all active filters in URL query params. Calls `GET /marinas/{id}/slips/search`. Click a slip → `/slips/{slipId}` (existing detail page).
- See `docs/marketplace.md > Discovery & search` for full algorithm, including `RateKind = "Mixed"` rule (show "from $X" not range) and demo-tenant filter.

### Onboarding wizard
- `/marina/new` → POST creates draft Marina (`IsSetupComplete=false`, `SetupStep=1`) → redirect to `/marina/{id}/setup`.
- Wizard step advance → POST current step's data → bump `SetupStep`. On direct navigation to `/marina/{id}/setup`, resume at saved `SetupStep`.
- **Crash recovery:** every input write → localStorage `marina-setup-{marinaId}` synchronously. Backend sync on step transitions + "Save progress" button. On load, compare localStorage timestamp vs backend `UpdatedAt`; newer wins.
- **Step 2 geocoder:** Nominatim with progressive fallback (full → city+state+zip → city+state → state). Display precision badge per result. Always show map for manual pin placement, even on no-result.
- **Step 3 generators:** Pure functions `generateDockName(convention, index, config)` and `generateSlipName(convention, dockIdx, slipIdx, totalSlipsBefore, config)`. Adding a convention = one new function + one UI case, no caller changes.
- **Step 4 saves:** `PUT /marinas/{id}/setup/docks` — atomic replace of the whole draft tree. Same contract as future spreadsheet-import feature. Rejected with 409 on non-draft marinas.
- **Step 5 publish:** Toggle defaults OFF. On submit set `IsSetupComplete=true`, `IsListed=toggle`. Redirect to marina dashboard.

### Reservation status flow
Reservation transitions: `PendingHostMarinaApproval` → `PendingApproval` → `Confirmed` → `Completed` (or `Cancelled` / `Declined` / `NoShow` at various stages). Initial state depends on `Slip.HostMarinaPolicy` and `AvailabilityWindow.InstantBook`. See `docs/marketplace.md > Reservation lifecycle` and `docs/data-model.md#reservation-status-transitions`.

### "I'm Away" / sublet
Three listing sources: `ListedByKind = Owner` (default), `Holder` (gated on `SlipAssignment.AllowHolderSublet`), `OwnerForHolder` (gated on `SlipAssignment.AllowOwnerSubletWhenAway` + active "I'm Away"). Revenue split snapshotted to `Reservation.RevenueSplitSnapshot` at booking, immutable thereafter. See `docs/marketplace.md > Sublet flows`.

## Source of Truth — Read These Before Implementing

These are in the repo at `PatrickBig/my-marina@main`. The wireframes are derived from them; **always defer to them** when implementing:

| File | What it covers |
|---|---|
| `docs/overview.md` | Vision, personas, identity model |
| `docs/marketplace.md` | Search algorithm, listing creation, reservation lifecycle, sublet flows, pricing, revenue split |
| `docs/features/boaters.md` | Boater-side MVP feature matrix |
| `docs/features/marina-operators.md` | Operator-side MVP feature matrix |
| `docs/features/private-slip-owners.md` | Private-host wizard, single-slip flows |
| `docs/features/platform-operators.md` | Platform console scope |
| `docs/vessels.md` | Vessel / MarinaVesselRecord / ghost-claim flow |
| `docs/data-model.md` | Entity schemas, status enums, field-level definitions |
| `docs/auth-and-permissions.md` | JWT claim shape, Membership / BillingAccountMember junctions |
| `openspec/changes/marina-onboarding-wizard/proposal.md` | Wizard rework summary |
| `openspec/changes/marina-onboarding-wizard/specs/marina-onboarding-wizard/spec.md` | Draft state, home entry points, wizard routing, geocoder, crash recovery, publish |
| `openspec/changes/marina-onboarding-wizard/specs/dock-slip-bulk-setup/spec.md` | Naming conventions, dock-level defaults, preview/adjust, batch endpoint |
| `openspec/changes/marina-onboarding-wizard/specs/slip-amenities/spec.md` | New `HasPumpOut` / `IsCovered` / `IsIndoor` booleans + custom `Amenities[]` jsonb |
| `openspec/changes/marina-onboarding-wizard/tasks.md` | Suggested implementation task ordering |

## Design Tokens — Do Not Use

The wireframes intentionally use tokens that should **not** carry to production. Reach for the codebase's existing design system instead. The wireframe tokens listed only for completeness:

- Paper background `#fbf8f1`, ink `#1c1a17`, paper lines `#e6e0d3`
- Accent `oklch(60% 0.13 230)` — focused element only, not a brand color
- Highlighter yellow `oklch(94% 0.13 95)` — annotation use only
- Fonts: Caveat / Kalam / Architects Daughter / JetBrains Mono — wireframe aesthetic only

## Assets

No production assets in this handoff. All imagery is placeholder striped boxes. Slip / marina photos, logos, and brand assets are out of scope.

## Files Included

- `MyMarina Wireframes.html` — the canvas root; open this
- `design-canvas.jsx` — pan/zoom canvas component (third-party scaffold)
- `tweaks-panel.jsx` — runtime control panel (third-party scaffold)
- `wireframe-primitives.jsx` — sketchy UI primitives (WFBox, WFCard, WFAppBar, etc.)
- `frames-boater.jsx` — boater frames (incl. both search steps)
- `frames-operator.jsx` — marina-operator frames
- `frames-onboarding.jsx` — commercial onboarding wizard frames
- `frames-other.jsx` — private-host, platform-operator, cross-cutting frames

The `.jsx` files are referenced for completeness; implementation should be driven from the README + docs, not by transliterating these scripts.

## Suggested Implementation Order

1. **Slip amenities migration first** (small, additive) — `HasPumpOut`, `IsCovered`, `IsIndoor` columns + `Amenities` jsonb on `Slip` entity. Update `DemoSeedScript` in the same PR.
2. **Marina draft state** — `IsSetupComplete`, `SetupStep` columns + global query filter excluding drafts from public queries.
3. **Wizard step 1 (Profile)** — replace flat onboarding page. Creates draft Marina, redirects to `/marina/{id}/setup`.
4. **Wizard step 2 (Location)** — extract Leaflet draggable-pin component from existing `SearchPage` for reuse, build Nominatim geocoder utility with fallback chain.
5. **Wizard step 3 + 4 (Builder & Preview)** — naming-convention generator utilities, `PUT /marinas/{id}/setup/docks` batch endpoint, preview table.
6. **Wizard step 5 (Publish)** — toggle + activation.
7. **Home page entry points** — banner + draft card.
8. **Marketplace search rework** — bbox-based `MarinaSearchResultDto`, marina rollup endpoint, `/search/marinas/{id}` route.

This roughly matches the order in `openspec/changes/marina-onboarding-wizard/tasks.md`.
