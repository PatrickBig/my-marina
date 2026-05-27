# Screens · Customers & money

Customers · Assignments · Billing.

---

## Customers

`/marina/:marinaId/accounts`

This is the BillingAccount list — labelled "Customers" in the nav for plain-English
clarity. The underlying entity is `BillingAccount`.

### URL params

| Param | Default | Values |
|---|---|---|
| `status` | `all` | `all / active / overdue / dockominium / invited` |
| `id` | undefined | Selected account id (drawer open) |
| `page` | `1` | 1-indexed |
| `q` | undefined | Free-text search |

### Layout

Two-column at ≥ 1100 px: account table on the left, detail drawer on the right.
At narrower widths, drawer becomes a sheet on row click.

### Filter chip row + search

```tsx
<div className="row">
  <Input placeholder="Search by name, email, or vessel…" />
  {/* All, Active, Overdue, Dockominium, Invited */}
</div>
```

The Overdue chip is just a client-side filter over the same dataset. If the
backend offers a server-side `status` filter, use it.

### Table

| Column | Notes |
|---|---|
| Account | Avatar + name + email |
| Slip / Plan | Monospace — e.g. "A-12 transient", "C-7 (claim pending)" |
| Balance | Right-aligned, monospace, red + bold if overdue, "—" if account has no slip |
| Vessels | Comma list of vessel names. "GHOST" if any unclaimed. |
| Status | Badge — Open / Active / Overdue / Invited / Dockominium |

Row click sets `?id=…` and opens the drawer. Selected row gets a subtle primary
tint background.

### Drawer

Header: avatar + name + email + member count + close button.

If overdue: a **red-tinted callout block** showing total overdue amount and last
reminder date.

Sections (separator between each):

1. Members — list of `BillingAccountMember` with role.
2. Vessels — list of vessel records with insurance status / expiry.
3. Open invoices — list of two or three with id, amount, age. Click → navigates
   to `billing?id=<inv>`.
4. Action row: Invoice / Payment / Message.

### Data

- `getBillingAccounts(marinaId)` for the list.
- `getBillingAccountMembers`, `getVesselRecords`, `getMarinaInvoices` for the
  drawer.
- The existing `BillingAccountDetail` component in `MarinaDashboardPage.tsx` is
  a great starting point — lift it out and trim it.

---

## Assignments

`/marina/:marinaId/assignments`

The active leases table — `SlipAssignment` entities filtered to `isActive`.

### URL params

| Param | Default | Values |
|---|---|---|
| `type` | `all` | `all / annual / seasonal / monthly / transient` |
| `page` | `1` | 1-indexed |
| `q` | undefined | Free-text search |
| `endingSoon` | undefined | If `true`, filter to leases ending in next 30 days. |

### Table

| Column | Notes |
|---|---|
| Slip | Monospace + bold |
| Account | Plain text |
| Vessel | Muted |
| Type | Badge — Annual / Seasonal / Monthly / Transient |
| Term | `2026-05-01 → 2026-10-31` or `2026-01-01 → open-ended` |
| Rate | Right-aligned monospace |
| Sublet policy | Badge if non-empty (Owner only / Holder only / Owner+Holder) |
| Actions | Edit |

### Filter chips

All · Annual · Seasonal · Monthly · Ending soon.

### Pagination

`pageSize` = 8 rows. Use `<Pagination>` at the bottom.

### Add / edit assignment

Use the existing `AssignmentsPanel` form from the mega-page — it's already
react-hook-form + Zod. Move it to a Radix `<Dialog>` triggered by the page
header's "+ New assignment" button. Edit is a row action that opens the same
dialog with the row's data.

### Data

- `getSlipAssignments(marinaId, { activeOnly: true })`
- `createSlipAssignment`, `updateSlipAssignment`, `endSlipAssignment`

---

## Billing

`/marina/:marinaId/billing`

**This is a new screen.** It's invoice-focused (not account-focused — that's
Customers).

### URL params

| Param | Default | Values |
|---|---|---|
| `status` | `all` | `all / open / overdue / partial / paid / voided` |
| `id` | undefined | Selected invoice id |
| `page` | `1` | 1-indexed |
| `q` | undefined | Free-text search |

### Layout

KPI tile row (4 tiles, full-width at narrower):

1. **Outstanding** — sum of (amount − paid) for Open + Overdue + Partial. Big
   number, "across N invoices" hint.
2. **Overdue** — sum + count + age of oldest. Number colour: destructive.
3. **MTD collected** — sum of payments this month. Delta badge vs last month.
4. **Aging buckets** — mini bar chart with 4 buckets: Current / 1–30 d / 31–60 d
   / 60+ d. Each bar tinted by severity.

### Filter chip row + search

```
[Search] All · Open · Overdue · Partial · Paid · Voided
```

Chip counts are derived from the same dataset, so they update live with
mutations.

### Table

| Column | Notes |
|---|---|
| Invoice | Monospace + bold |
| Account | Plain text |
| Slip / line | Muted |
| Issued | Muted |
| Due | Muted; overdue rows show red age underneath ("12d overdue") |
| Amount | Right-aligned monospace + bold. Partial shows "$x of $y" |
| Status | Badge (per status) |
| Actions | **Context-sensitive** — Overdue: Remind. Open/Due soon/Partial: Record (payment). Paid/Voided: View. |

Voided rows render at ~55% opacity.

### Aging bar component

```tsx
function AgingBars({ buckets }: Props) {
  const max = Math.max(...buckets.map(b => b.amount));
  return (
    <div className="flex flex-col gap-1 mt-2">
      {buckets.map(b => (
        <div key={b.label} className="flex items-center gap-1.5">
          <div className="w-14 text-[11px] text-muted-foreground">{b.label}</div>
          <div className="flex-1 h-2 bg-muted rounded overflow-hidden">
            <div className="h-full" style={{ width: `${(b.amount/max)*100}%`, background: b.tone }} />
          </div>
          <div className="w-14 text-right text-[11px] font-mono">
            ${b.amount.toLocaleString()}
          </div>
        </div>
      ))}
    </div>
  );
}
```

### Header actions

Three buttons in this order:

1. Send reminders (secondary) — opens a dialog that pre-selects overdue accounts.
2. Record payment (secondary) — opens an "Apply payment" dialog.
3. **New invoice** (primary) — opens the invoice composer.

### Detail behavior

Selecting an invoice (row click → `?id=<inv>`) opens a right-side drawer
similar to Customers. The drawer shows:

- Header: Invoice number + account + status badge
- Line items with amounts
- Payment history (paid / partial entries)
- Action row: Mark paid / Apply partial / Void / Send PDF (PDF disabled, label
  "post-MVP")

### Data

- `getMarinaInvoices(marinaId, { status })` — server-side filter when possible.
- `createInvoice`, `recordPayment`, `voidInvoice`, `sendInvoice` — mutations
  invalidate `['marina-invoices', marinaId]` and `['marina-counters', marinaId]`.

### KPI maths

```ts
const outstanding = invoices
  .filter(i => ['Sent','DueSoon','Overdue','Partial'].includes(i.status))
  .reduce((sum, i) => sum + (i.amount - i.paid), 0);

const overdueAmt = invoices
  .filter(i => i.status === 'Overdue')
  .reduce((sum, i) => sum + (i.amount - i.paid), 0);

const mtdCollected = payments
  .filter(p => isCurrentMonth(p.paidOn))
  .reduce((sum, p) => sum + p.amount, 0);
```

Ideally these come from a single `getBillingSummary(marinaId)` server endpoint so
the page doesn't have to download every invoice to compute the KPIs. If that
endpoint doesn't exist, derive client-side initially and file a follow-up.

### Mobile

Below 640 px the invoice table converts to stacked cards (see
[`design-system.md`](./design-system.md#mobile-stacked-table)). KPI tiles
collapse 4 → 2 → 1 columns as width tightens.
