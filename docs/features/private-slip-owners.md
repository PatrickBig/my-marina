# Feature Area: Private Slip Owners

Private slip owners are individuals who own one or a small number of slips and want to rent them out — either at their own waterfront property (private dock) or at a marina where they own a slip outright (dockominium). The platform onboards them as Free-tier hosts, with their own single-slip Marina behind the scenes — but the UX is branded around "your dock" or "your slip," never "your marina."

> Closely related to [features/marina-operators.md](./marina-operators.md), but lighter and re-branded. Many host-side features are shared; this doc highlights what's distinctive.

---

## Onboarding

**Goal:** Stand up a private host in under 2 minutes.

| Feature | Description | MVP |
| --- | --- | --- |
| "Add my dock" wizard | For private dock owners (no host marina) | Yes |
| "Add a slip I own at a marina" wizard | For dockominium owners (with host marina selection) | Yes |
| Auto-provision Tenant + Marina + Slip | Free tier, single-slip Marina, owner Membership; UX says "your dock" / "your slip" | Yes |
| Skip dock concept | Single slip; no dock layer to manage | Yes |
| Geographic location | Lat/long auto-derived from address; user can refine on map | Yes |
| Dockominium host selection | If host marina is on the platform, it's auto-suggested when address matches | Yes |
| Confirm `HostMarinaPolicy` | Default `NotifyOnly`; explain options | Yes |
| Photos at onboarding | 1–4 slip / property photos before first listing | Yes |

---

## Slip Configuration

**Goal:** Set up the one slip's specs.

| Feature | Description | MVP |
| --- | --- | --- |
| Slip dimensions | Max length, beam, draft | Yes |
| Slip type | Floating / Fixed / Mooring / Anchorage | Yes |
| Amenities | Electric (with amperage), water | Yes |
| Photos | Slip and access photos (3–8) | Yes |
| Description | Free-text notes on access, parking, gate code instructions, etc. | Yes |
| Multiple slips at same property | Add additional slips under the same Marina | Yes (rare but supported) |

---

## Marketplace Listings

**Goal:** List the slip on the marketplace; manage availability.

| Feature | Description | MVP |
| --- | --- | --- |
| Calendar editor | Drag a date range, set price/policy | Yes |
| Pricing | Base/night, weekly discount, monthly discount, cleaning fee, min/max nights | Yes |
| Instant book vs request | Per-window toggle | Yes |
| Pause / close listing | Temporarily hide a window | Yes |
| Block-out periods | Mark dates as unavailable (no listing, no booking) | Yes |
| Templates | "My summer pricing," "My fall pricing" | No (post-MVP) |

---

## Host Marina Policy (Dockominium only)

**Goal:** Coordinate with the physical-location marina.

| Feature | Description | MVP |
| --- | --- | --- |
| Choose policy | None / NotifyOnly (default) / RequiresApproval | Yes |
| See policy changes from host marina | Host marina can request a policy upgrade; owner approves | Yes |
| Rebut a host marina decline | Owner sees declined reservations; can contact host marina out-of-band | Yes |
| Host marina-fee deduction | Per-booking fee that the host marina charges (e.g., gate-access provisioning); deducted from the owner's revenue via `RevenueSplit` with `payeeKind = HostMarina` | Yes (configurable) |

---

## Reservation Management

**Goal:** Approve, decline, manage incoming reservations.

| Feature | Description | MVP |
| --- | --- | --- |
| Reservation inbox | All reservations | Yes |
| Approve / decline | Request-to-book reservations | Yes |
| Cancel | Confirmed reservations (per policy) | Yes |
| Mark NoShow | After arrival window | Yes |
| Boater communication | Pre-arrival message, gate code, parking instructions | Yes (via Announcement to `IncomingBoaters` audience) |
| Pre-booking Q&A | Boater asks a question before reserving | No (post-MVP) |

---

## Earnings

**Goal:** Track what the slip is making.

| Feature | Description | MVP |
| --- | --- | --- |
| Earnings dashboard | Total reservations, gross revenue, host marina deductions, net | Yes |
| Per-month / per-year breakdown | Calendar slicing | Yes |
| Off-platform reminder | "Era 1: you'll invoice the boater directly. Set up payouts when Stripe Connect launches." | Yes |
| Stripe Connect payouts | Online payments + auto-payouts | No (Era 2) |

---

## Customer Relationship at Host Marina (Dockominium only)

**Goal:** A dockominium owner is also a customer of the host marina (HOA fees, electric, services).

| Feature | Description | MVP |
| --- | --- | --- |
| BillingAccount at host marina | Auto-created if the host marina invites; manual link otherwise | Yes |
| View HOA invoices from host marina | Through the standard `BillingAccount` view | Yes |
| Submit maintenance requests to host marina | Standard boater flow | Yes |
| Receive announcements from host marina | Standard announcement flow | Yes |

This is the "two roles at the same physical site" case — see [marketplace.md > Cross-role considerations](../marketplace.md#cross-role-considerations). The platform handles it by surfacing both contexts on a unified dashboard, no toggle needed.

---

## Notes

- The Tenant + Marina + Slip auto-creation is bookkeeping. Users see "your dock" / "your slip"; they don't see "your tenant" or "your marina."
- Free-tier private hosts get the basic listing and reservation toolkit. Premium hosts get advanced pricing, multi-window templates, and (eventually) priority placement in search.
- Private hosts share the staff-management surface but most won't use it — single-owner is the common case.
- A private host is a `Membership` Owner of their own Marina. They're not a "platform operator" or anything special; just a host with a one-slip inventory.
