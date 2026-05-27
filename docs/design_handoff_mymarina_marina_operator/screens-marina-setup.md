# Screens · Marina setup

Slips & docks · Pricing plans · Announcements · Staff · Settings.

---

## Slips & docks

`/marina/:marinaId/slips`

The dock + slip inventory page. Existing `MarinaSlipsPage.tsx` is close but
needs lifting into the workspace and getting pagination.

### URL params

| Param | Default | Values |
|---|---|---|
| `dock` | first dock id | Dock filter rail selection |
| `status` | `active` | `active / maint / inactive / listed / all` |
| `plan` | undefined | Pricing plan filter (deep-link from Pricing) |
| `page` | `1` | 1-indexed |

### Layout

Two-column at ≥ 900 px: dock filter rail (220 px) + slip table.

Rail = vertical list of dock cards, each showing:

- Dock name + filled/total ratio (monospace).
- Note line (e.g. "Long-term · 30A/50A", "Transient · floating").
- A 4 px progress bar tinted by occupancy.
- Selected dock gets the `selected` state.

Below 900 px the rail becomes an auto-fill grid of dock cards above the table.
Below 640 px it becomes a single-column flex stack.

### Table

| Column | Notes |
|---|---|
| Slip | Monospace bold |
| Type | Floating / Fixed / Mooring / DryStorage / Anchorage |
| Max L×B×D | Monospace |
| Power | Plain |
| Status | Badge: Active (success) / Maint (warning) / Inactive (neutral) |
| Assignment | Plain text — current holder + lease type, or "Vacant · listed", "—" |
| Actions | Edit (opens existing SlipForm in a Dialog) |

### Filter chips + search

`[Search Dock A…]   Active · Maint · Inactive · Listed · All`

If `plan` is set (e.g. from Pricing → "View applied slips"), show a clear-pill
banner above the chips: "Filtered by plan: Premium · 50A · 18 slips · Clear".

### Pagination

`pageSize` = 10 rows. Use `<Pagination>` at the bottom. Replace any "Load all"
links.

### Add slip / add dock

Page header has two buttons: "+ Dock" (secondary), "+ Slip" (primary). Both open
the existing forms in a Radix `<Dialog>`. Delete confirmation uses `<AlertDialog>`.

### Data

- `getSlips(marinaId)`, `getDocks(marinaId)`
- `createDock`, `updateDock`, `deleteDock`
- `createSlip`, `updateSlip`, `deleteSlip`

---

## Pricing plans

`/marina/:marinaId/pricing`

**This already exists** as `PricingPlansPage.tsx` (PR #40). Two changes:

1. Move it from a standalone page (its own NavBar + body) into the workspace
   shell — replace the page-level layout with `<PageHeader>` + `<PageBody>`.
2. Restyle the plan cards to match the workspace card pattern (selected-state,
   right-side preview panel).

### URL params

| Param | Default | Values |
|---|---|---|
| `id` | first plan id | Selected plan id |
| `mode` | `view` | `view / edit / new` |
| `bulk` | undefined | If set to a plan id, open the bulk-assign dialog. |

### Layout

Two-column at ≥ 1100 px: plan list (cards) + preview / editor sidebar.

### Plan card

Each card shows:

- Header row: small dollar icon + name + Default badge + slips-applied count
  (with link to `/slips?plan=<id>`).
- Actions: Set default (hidden if already default), Bulk assign, kebab menu
  (Edit / Delete).
- Body grid (3 columns): Transient rate · Lease rate · Add-on chips.

### Preview sidebar

Stays visible at all times; shows the selected plan's rates applied against
three example slips of varying size and amenity set:

```
28' × 10'   $90/night
            Lease: $1,200/mo

38' × 12'   $133/night   (+ $8 amenity)
            Lease: $1,260/mo

50' × 16'   $175/night   (+ $20 amenity)
            Lease: $1,860/mo
```

The math should mirror the server-side pricing calculation. There is already a
`PricingPreviewPanel` component in the codebase — reuse it.

### Bulk-assign dialog

The existing `BulkAssignDialog` in `PricingPlansPage.tsx` is good. Wire it to
the workspace-styled cards.

### Data

- `getPricingPlans`, `createPricingPlan`, `updatePricingPlan`,
  `deletePricingPlan`, `setDefaultPricingPlan`, `bulkAssignPricingPlan` — all
  already exist.

---

## Announcements

`/marina/:marinaId/announcements`

Lift-and-shift from the existing `AnnouncementsPanel` in `MarinaDashboardPage`.

### URL params

| Param | Default | Values |
|---|---|---|
| `status` | `all` | `all / published / draft` |
| `id` | undefined | Selected announcement (for edit) |

### Layout

Stacked card list. Pinned items always at top with a Pin icon. Each card shows:

- Header row: optional Pin icon + title + status badge (Published / Draft).
- Audience + posted date.
- Body excerpt.
- Right-side: Edit / Publish (for drafts) / kebab.

### Page header

Single primary action: + New announcement.

### Data

- `getMarinaAnnouncements(marinaId)`, `createAnnouncement`, `publishAnnouncement`,
  `deleteAnnouncement`.

---

## Staff

`/marina/:marinaId/staff`

Plain table — same shape as Customers minus the drawer.

### Table

| Column | Notes |
|---|---|
| Person | Avatar + name + email |
| Role | Owner (primary badge) / Manager (accent) / Staff (neutral) |
| Scope | "Tenant · 3 marinas" / "Big Bay Marina" / "Big Bay · billing only" |
| Last active | "2h ago" / "Invite pending" badge for unaccepted invites |
| Actions | Kebab — Resend invite / Change role / Revoke |

### Page header

Single primary action: Invite staff (opens email-invite dialog).

### Data

- `getMarinaStaff(marinaId)`, `inviteStaff`, `revokeStaff`.

### Note

The existing model has Owner / Manager / Staff roles. Granular per-area
permissions (billing-only / maintenance-only) are post-MVP — include a footnote
in the page subtitle that mentions this.

---

## Settings

`/marina/:marinaId/settings`

**This is a new screen.** It absorbs the existing `MarinaInfoPanel` and adds
photos, hours, and a subscription summary.

### URL params

| Param | Default | Values |
|---|---|---|
| `tab` | `profile` | `profile / address / hours / photos / subscription` |

### Layout

Sub-tab strip below the `PageHeader` (the only screen using sub-tabs):

- Profile · Address & map · Hours & policy · Photos · Subscription

Each tab renders into a single `<Card>` with form rows in a two-column grid
(label/help on the left, inputs on the right). Below 900 px the grid collapses
to a single column.

### Profile tab

| Field | Input |
|---|---|
| Marina name | Text |
| Marina type | Select — Commercial / YachtClub / PrivateCommunity / Dockominium / PrivateDock (changing is rare) |
| Contact | Email + Phone (two-column) |
| Website | Text |
| Description | Textarea |

### Address tab

| Field | Input |
|---|---|
| Street | Address line 1 + line 2 |
| City / State / Zip | Three columns |
| Coordinates | Lat + Lon (two columns) with an auto-fill button ("📍 Auto-fill from address") that geocodes via the existing nominatim call. |
| Map preview | `<MapPicker>` (existing component) with the current pin draggable. |
| Timezone | Select |

### Hours tab

| Field | Input |
|---|---|
| Summer hours | Open + close + days of week |
| Off-season hours | Open + close + days of week |
| Approval policy | Select — Instant book / Request to book |
| Auto-decline | Select — 24 / 48 / 72 / Never |

### Photos tab

Grid of photo tiles (auto-fill, min 200 px). Each tile:

- Drag handle (top-right) to reorder.
- Remove button (top-right).
- "Cover" badge on the first tile (the listing cover photo).
- Label + dimensions overlay on the bottom-left.

Upload button in the header opens the existing `CropUploadModal` flow.

### Subscription tab

| Field | Value |
|---|---|
| Plan tile | Big icon + name (Free / Pro / Premium) + renew date + price + Change plan button |
| Feature matrix | Two-column list — "Marketplace listings · Unlimited", "Advanced reporting · Premium only" (greyed if not in current plan) |

### Save

Page-level primary action in the header: "Save changes". Disabled until form is
dirty. Saves all of the currently visible tab. Uses the existing
`updateMarina(marinaId, …)` call.

### Data

- `getMarina(marinaId)`, `updateMarina(marinaId, …)`
- Photos use `usePhotoUpload` and the photo asset endpoints already in the
  codebase.

### Note

The current `MarinaInfoPanel` already covers Profile + Address well — start by
moving it intact into the Profile + Address tabs, then add Hours, Photos, and
Subscription as new tabs.
