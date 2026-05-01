# MyMarina — Marketplace

> Cross-references: [overview.md](./overview.md) for the vision, [data-model.md](./data-model.md) for entity schemas, [auth-and-permissions.md](./auth-and-permissions.md) for authorization, [glossary.md](./glossary.md) for terminology.

## Scope

The marketplace is the boater-facing layer of MyMarina. It covers slip discovery, listing creation, the reservation lifecycle, sublet flows, host approval, pricing, and (future) payment routing.

This document describes the **business logic** of the marketplace. The data shapes are in [data-model.md](./data-model.md); auth flows are in [auth-and-permissions.md](./auth-and-permissions.md); the UI patterns will land in feature docs.

---

## Slip ownership scenarios — end-to-end

All slips are pinned to a `Marina`. Real-world ownership is encoded by `Marina.MarinaType` and (for dockominiums) `Slip.HostMarinaId`. Each scenario below shows the full path from slip creation to a reservation completing.

### 1. Marina-owned slip (default)

**Setup.** A commercial marina (`MarinaType = Commercial`) creates docks and slips during onboarding. Slips belong to the marina.

**Listing.** Marina staff create an `AvailabilityWindow(ListedByKind = Owner, ListedByMarinaId = <marinaId>)` covering, e.g., July 1 – Aug 31, $4/ft/night, instant-book on.

**Reservation.** Boater searches "slips near Annapolis Aug 5–8" and finds the listing. They book; reservation auto-confirms. Marina sees the booking on their dashboard, prepares the slip, greets the boater on arrival.

**Payment (MVP).** Marina invoices the boater via the existing manual invoice flow. Boater pays the marina directly.

### 2. Yacht club / HOA slip

**Setup.** Same as Marina-owned but `MarinaType = YachtClub` or `PrivateCommunity`. Members hold permanent assignments; transient guest dockage may be available between assignments.

**Listing.** Yacht-club admin lists transient slots when slips are open between member assignments.

**Reservation.** Same as Marina-owned. The yacht club may pre-screen guests via `InstantBook = false` if their bylaws require approval.

### 3. Dockominium slip

**Setup.** Maria owns slip A-12 outright at Big Bay Marina. She signs up for MyMarina via "Add a slip I own at a marina":

1. Free-tier `Tenant` is created in Maria's name.
2. `Marina` is created with `MarinaType = Dockominium`, named "Maria's slip at Big Bay" (or whatever Maria calls it).
3. `Slip` A-12 is created in Maria's marina with `HostMarinaId = <Big Bay Marina>` and `HostMarinaPolicy = NotifyOnly` (the default).
4. Maria gets Owner `Membership` at her own marina.

If Big Bay is also on the platform, their staff are notified that a dockominium slip has been registered at their location. They may request a `HostMarinaPolicy = RequiresApproval` upgrade if their HOA bylaws need it; Maria approves the policy change.

**Listing.** Maria creates an `AvailabilityWindow(ListedByKind = Owner, ListedByMarinaId = <Maria's marina>)` for her vacation weeks.

**Reservation.** Boater finds Maria's slip in search. Booking flow:

- If `HostMarinaPolicy = NotifyOnly`: reservation goes per the AvailabilityWindow's `InstantBook` setting. Big Bay is notified (informational).
- If `HostMarinaPolicy = RequiresApproval`: reservation enters `PendingHostMarinaApproval`. Big Bay reviews; approves or declines. If approved AND `InstantBook = true` → `Confirmed`; if approved AND `InstantBook = false` → `PendingApproval` with Maria.

**Payment (MVP).** Maria invoices the boater via *her* marina's invoicing surface. Big Bay does not invoice the boater for the slip rental — Big Bay's relationship is with Maria (HOA-style fees, billed separately).

### 4. Private dock

**Setup.** Pat owns waterfront property with their own dock. They sign up via "Add my dock":

1. Free-tier `Tenant`.
2. `Marina` with `MarinaType = PrivateDock`, named "Pat's dock" or similar.
3. `Slip` with `HostMarinaId = null`. Lat/long is the home address.
4. Pat gets Owner `Membership` of their marina.

**Listing.** Pat creates an `AvailabilityWindow`.

**Reservation.** Same as Marina-owned, no host-marina interactions.

**Payment.** Pat invoices.

---

## Discovery & search

### Search inputs

A boater searching for a slip provides:

- **Where** — geographic point (latitude, longitude) plus a radius. Defaults to "near current location" using browser geolocation.
- **When** — arrival and departure date/time.
- **Boat fit** — length, beam, draft (auto-filled from the boater's selected `Vessel`).
- **Filters (optional)** — slip type (Floating, Mooring, etc.), amenities (electric, water), maximum price.

### Search algorithm (MVP)

1. **Bounding-box query** against `Slip.Latitude` / `Slip.Longitude`. Pre-filter slips within an axis-aligned box that conservatively covers the requested radius.
2. **Vessel-fit filter:** `Slip.MaxLength ≥ vessel.Length` AND `Slip.MaxBeam ≥ vessel.Beam` AND `Slip.MaxDraft ≥ vessel.Draft`.
3. **Amenity & type filters** if specified.
4. **Active listing filter:** at least one `AvailabilityWindow` (`Status = Open`) covers the requested arrival–departure range.
5. **Conflict filter:** no overlapping `Reservation` (status in `Confirmed`, `PendingApproval`, `PendingHostMarinaApproval`) for the requested range.
6. **Lease filter:** the slip is not blocked by an active `SlipAssignment` *unless* the assignment exposes a Holder or OwnerForHolder availability window covering the request.
7. **Distance refinement:** apply Haversine distance against the search point to drop the bounding-box false positives.
8. **Sort:** primary by distance, secondary by price.
9. **Paginate:** return slips with photos, price, dimensions, distance, host name, and instant-book flag.

### Why bounding box, not PostGIS

A bounding-box query is pure Postgres. PostGIS provides true geographic distance, polygon containment, and routing — all overkill for "find me a marina within 25 miles." We accept ~5–10% false positives at the edges and filter them with a Haversine calculation post-query. This keeps the dependency surface small and is fast enough at MVP scale.

If/when search complexity grows (e.g., "within 10 nautical miles along navigable water"), PostGIS becomes an upgrade target.

### Search result shape

A search result is a list of `SlipSearchResultDto` records, each carrying a representative `AvailabilityWindow` (the cheapest one that fits the requested dates). Multi-window pricing details are loaded on the slip detail page.

---

## Listing — creating an AvailabilityWindow

A host creates listings by carving up the slip's calendar with `AvailabilityWindow` records. Each window:

- Covers a date range (`StartsAt` / `EndsAt`)
- Defaults to `Status = Open`; can be `Paused`, `Closed`, or auto-set to `FullyBooked` when capacity is consumed
- Has `InstantBook` on/off
- Has min/max-night constraints
- Has a base price-per-night, optional weekly/monthly discounts, optional cleaning fee
- Carries a snapshot of the revenue split (immutable once first booking lands)

Windows on the same slip cannot overlap. Application logic rejects overlapping creates with a 409 Conflict.

The listing UX in `MyMarina.Web` resembles a calendar where the host highlights a range and sets price/policy. Behind the scenes this writes one or more `AvailabilityWindow` records.

### Listing visibility

Boaters see windows where `Status = Open`. Windows in `Paused`, `Closed`, or `FullyBooked` are hidden from search results.

---

## Reservation lifecycle

### Status states

| Status | Meaning |
| --- | --- |
| `PendingHostMarinaApproval` | Awaiting host marina approval (only when `Slip.HostMarinaPolicy = RequiresApproval`) |
| `PendingApproval` | Awaiting slip owner's approval (request-to-book windows) |
| `Confirmed` | Locked in. Both sides committed. |
| `Declined` | Owner or host marina rejected the request. |
| `Cancelled` | Confirmed reservation later cancelled by either party. |
| `Completed` | Auto-set after `DepartsAt` passes without incident. |
| `NoShow` | Host marks no-show after `ArrivesAt` window with no boater arrival. |

### Status transitions

See [data-model.md#reservation-status-transitions](./data-model.md#reservation-status-transitions) for the full table. Summary:

- A new reservation enters `PendingHostMarinaApproval`, `PendingApproval`, or `Confirmed` based on the slip's `HostMarinaPolicy` and the window's `InstantBook` setting.
- `PendingHostMarinaApproval` flows to `PendingApproval` (if `InstantBook = false`) or directly to `Confirmed` (if `InstantBook = true`) once the host marina approves.
- `PendingApproval` flows to `Confirmed` or `Declined` based on the slip owner's decision.
- `Confirmed` flows to `Cancelled` (either party), `Completed` (auto), or `NoShow` (host action).

### Action capabilities by role

| Action | Boater (`BoaterUserId`) | Slip owner (`Membership` at `Slip.MarinaId`) | Host marina staff (`Membership` at `Slip.HostMarinaId`) |
| --- | --- | --- | --- |
| Submit reservation | ✓ | n/a | n/a |
| Approve / Decline (request-to-book) | ✗ | ✓ | ✗ |
| Approve / Decline (host marina policy) | ✗ | ✗ | ✓ (only when policy = RequiresApproval) |
| Cancel before arrival | ✓ (per cancellation policy) | ✓ | ✗ |
| Mark NoShow | ✗ | ✓ | ✗ |
| Override (force confirm/cancel) | ✗ | ✓ (Owner role only) | ✗ |

---

## Sublet flows

### "I'm away" (boater self-service)

A long-term lease holder marks a date range when they won't be using their slip. The flow:

1. Boater opens "My Slip" in the portal.
2. Taps **"I'll be away,"** picks `start` / `end`.
3. The system records the absence on the slip's calendar, visible to `Slip.MarinaId` staff.
4. Behavior depends on `SlipAssignment.AllowOwnerSubletWhenAway`:
   - **`true`:** Marina staff are notified and can choose to create an `AvailabilityWindow(ListedByKind = OwnerForHolder, RelatedAssignmentId = <this>)`. Revenue split: marina takes their share, holder gets `OwnerSubletShareToHolder` of the gross.
   - **`false`:** Absence is informational only (security/yard tracking). No window can be created against it.
5. Independently, if `SlipAssignment.AllowHolderSublet = true`, the holder can create their own `AvailabilityWindow(ListedByKind = Holder, ListedByBillingAccountId = <holder's BillingAccount>)` directly. Revenue split: holder gets the bulk, marina takes `HolderSubletShareToOwner`.

### Holder sublet (boater initiates)

The current lease holder lists their leased slip themselves, subject to lease policy. Use cases:

- "I'm going to Bermuda for a month — let me recoup some of my slip fees."
- "I bought a seasonal slip but only use it on weekends — sublet weekdays."

The holder creates an `AvailabilityWindow` from their portal. Pricing is up to the holder (often higher than the marina's transient rate). Marina is notified. Revenue split is governed by the lease.

### Owner sublet of leased slip (marina initiates)

The marina lists a slip during the holder's known absence, with the holder's prior consent via `AllowOwnerSubletWhenAway`. Use cases:

- Marina maximizes utilization during low season.
- Marina knows a customer is gone for two weeks (via "I'm away").

Revenue is shared back to the holder per `OwnerSubletShareToHolder` — incentivizing the holder to flag absences honestly. This is a deliberate differentiator vs. existing marina software.

### Sublet conflict prevention

Application logic prevents:

- Two `AvailabilityWindow`s on the same slip overlapping in time
- A `Holder`-listed window outside the assignment's `StartDate` / `EndDate`
- An `OwnerForHolder` window unless the holder has an active "I'm away" entry covering that time
- A reservation against a sublet window outside the assignment's date range

---

## Host marina policy (dockominium approval)

When `Slip.HostMarinaId` is set (dockominium case), `Slip.HostMarinaPolicy` controls how the host marina participates:

| Policy | Reservation flow |
| --- | --- |
| `None` | Host marina is bypassed entirely. Owner's listing decisions stand. |
| `NotifyOnly` (default) | Host marina sees the reservation on their dashboard at creation time. Informational only; no approval gate. |
| `RequiresApproval` | Reservation enters `PendingHostMarinaApproval` first. Host marina must approve before the reservation can confirm (or move to `PendingApproval` for owner approval). |

Why this matters:

- A dockominium HOA may require background checks, boat-size verification, or gate-access provisioning before allowing transient guests.
- A laissez-faire arrangement (`None`) suits owners with full autonomy.
- `NotifyOnly` is the middle ground — host marina has visibility for security and operations without veto power.

The policy is set per-slip and can be negotiated between the slip owner and the host marina. Either party can request a change; the slip owner approves it (it's their slip).

---

## Pricing

### Window pricing

`AvailabilityWindow` carries the pricing primitive:

- `BasePricePerNight` — required
- `WeeklyDiscount` (0–1) — applied for stays ≥ 7 nights
- `MonthlyDiscount` (0–1) — applied for stays ≥ 28 nights
- `CleaningFee` — flat fee per reservation (optional)
- `MinNights` / `MaxNights` — stay-length constraints

### Reservation total computation

```text
nights         = (DepartsAt.Date - ArrivesAt.Date).Days
basePrice      = BasePricePerNight * nights
discount       = nights >= 28 ? MonthlyDiscount
               : nights >= 7  ? WeeklyDiscount
               : 0
discountedBase = basePrice * (1 - discount)
fees           = CleaningFee
taxes          = (discountedBase + fees) * marina.taxRate   // 0 in MVP unless configured
total          = discountedBase + fees + taxes
```

The reservation snapshots `BasePrice`, `Fees`, `Taxes`, and `Total` at booking time so subsequent price changes don't re-bill the boater.

---

## Revenue split & payment routing

### Split snapshot

When a reservation is created, the `AvailabilityWindow.RevenueSplit` is copied to `Reservation.RevenueSplitSnapshot`. Example shape:

```json
[
  { "payeeKind": "SlipOwner", "payeeId": "<marinaId>",         "percent": 0.85 },
  { "payeeKind": "Holder",    "payeeId": "<billingAccountId>", "percent": 0.10 },
  { "payeeKind": "Platform",  "payeeId": null,                 "percent": 0.05 }
]
```

`payeeKind` ∈ { `SlipOwner`, `Holder`, `HostMarina`, `Platform` }. The split is **immutable once the first reservation lands** on the window — even if the host edits the window's RevenueSplit later, existing reservations keep their snapshot.

### Era 1 — Off-platform payment (MVP)

Reservation `PaymentStatus = OffPlatform`. The platform records the booking but doesn't move money. The slip owner invoices the boater through the existing manual invoice flow.

For sublet bookings (Holder or OwnerForHolder), participants reconcile the split themselves:

- Marina-managed sublet: marina collects from the boater, applies the holder's share as a credit on the holder's next invoice or pays directly.
- Holder-managed sublet: holder collects from the boater; marina sends the holder an invoice for the marina's share.

The platform tracks the canonical split via `Reservation.RevenueSplitSnapshot` so disputes can be resolved by reference.

### Era 2 — Platform payment (post-MVP, Stripe Connect)

Reservation `PaymentStatus` flows: `Pending` → `Captured` (or `Refunded`).

1. Boater's payment method is captured at booking (or held with a hold-then-capture-on-arrival policy, configurable per host).
2. Funds sit in the platform's Stripe balance.
3. After arrival (or after a configurable delay), the platform creates Stripe transfers per the snapshot split:
   - SlipOwner share → Marina's `PayoutAccount`
   - Holder share → Holder's `PayoutAccount`
   - HostMarina share → Host marina's `PayoutAccount`
   - Platform share retained
4. Refunds reverse the splits proportionally.

`PaymentIntentId` and `PlatformFeeAmount` are reserved on `Reservation` from MVP day one; populated only in Era 2.

---

## Cancellation policy framework (sketched)

Detail is post-MVP. The shape:

- Each `AvailabilityWindow` references a `CancellationPolicyId` (a default policy until the host customizes).
- Policies define refund tiers — e.g., "Free cancellation up to 7 days before arrival; 50% refund 1–7 days; no refund within 24 hours."
- The reservation snapshots the active policy at booking time (`Reservation.CancellationPolicySnapshot`).
- Cancellation refunds (Era 2) follow the snapshot.

For MVP: the platform records cancellations and notes the snapshot, but no money moves. Hosts and boaters resolve refunds off-platform.

---

## Cross-role considerations

A user can hold multiple roles at the same physical site. The most common case:

### Dockominium owner who's also a host-marina customer

Maria owns slip A-12 at Big Bay (her dockominium). She also pays Big Bay for HOA dock fees, electric, pump-out — separate from any slip rental. She has:

- Owner `Membership` at *her own* marina (her dockominium marina, single slip).
- `BillingAccountMember` (typically Owner role) at Big Bay's `BillingAccount` for "Maria Rodriguez."

She sees both on her dashboard:

- Her marina (slip listings, reservations she hosts, revenue earned)
- Her customer relationship at Big Bay (her HOA invoices, dock-association announcements, maintenance requests against her slip's facilities)

The UI surfaces both contexts simultaneously — no toggle. Each entity (her marina, her billing-account membership) is a card on a unified dashboard.

### Marina owner who's also a transient boater

A marina owner who travels to other marinas as a guest is just a regular boater at those other marinas. They have Owner `Membership` at their home marina + boater-state at all others (no special relationship). Reservations they make as a boater appear under "My Trips," distinct from reservations *received* at their marina.

### Multi-billing-account customer

A boat-charter business may have BillingAccount memberships at 5 marinas they regularly visit. Each is a separate `BillingAccountMember` row; each appears on the user's dashboard.

---

## Open questions

- **Search ranking signals beyond distance and price.** Reviews (post-MVP), recent-booking frequency, host responsiveness rate, photo quality. Defer until we have enough listings to need ranking.
- **Currency / multi-currency.** MVP is USD-only. Multi-currency is post-MVP and requires care around split snapshots and FX-rate locking.
- **Same-day arrival cutoffs.** Should hosts be able to disable same-day reservations? Probably yes — add a per-window `AdvanceNoticeHours` field if needed.
- **Group reservations / multi-slip bookings.** Family flotillas reserving 5 adjacent slips. Each as separate Reservations? One bundled? Defer to post-MVP.
- **Calendar sync (iCal export).** Likely high-value for hosts with off-platform tools. Post-MVP.
- **Pricing rules engine.** Dynamic pricing based on demand, day-of-week, holidays. Post-MVP.
