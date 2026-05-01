# Feature Area: Marina Operators

Marina operators are the business owners and staff who manage a commercial marina, yacht club, or HOA-managed boating community on the platform. They use the full management toolkit and may also list slips on the marketplace.

> See [features/private-slip-owners.md](./private-slip-owners.md) for the dockominium / private-dock host experience, which is similar but lighter.

---

## Marina Setup & Configuration

**Goal:** Allow a marina to configure their physical layout and profile.

| Feature | Description | MVP |
| --- | --- | --- |
| Marina profile | Name, address, lat/long, contact info, description, timezone, hours, marina type | Yes |
| Dock management | Create/edit/delete docks; set sort order | Yes |
| Slip management | Add slips to docks; set size constraints, type, amenities, status | Yes |
| Slip status management | Mark slips as Active, UnderMaintenance, Inactive | Yes |
| Slip map / occupancy view | Visual grid of docks and slips with status/assignment at a glance | No (post-MVP) |

---

## Staff & Memberships

**Goal:** Allow marina owners to manage employee access.

| Feature | Description | MVP |
| --- | --- | --- |
| Invite staff | Send invitation by email; creates a pending `Membership(Marina)` | Yes |
| Staff roles | Owner, Manager, Staff (see [auth-and-permissions.md](../auth-and-permissions.md)) | Yes |
| Tenant-scoped Owners | Marina chains: one Owner Membership covers all marinas under the Tenant | Yes |
| Deactivate staff | Revoke Membership without deleting historical references | Yes |
| View staff list | All members and their roles | Yes |
| Granular permissions | Billing-only / maintenance-only / etc. roles | No (post-MVP) |

---

## BillingAccount Management

**Goal:** Manage the marina's customer base.

| Feature | Description | MVP |
| --- | --- | --- |
| BillingAccount list | Search, filter, paginate | Yes |
| BillingAccount detail | Contact info, members, vessels, current slip, invoice history, requests | Yes |
| Create BillingAccount | Manually add; optionally invite the contact email | Yes |
| Edit BillingAccount | Update contact info, notes, billing address | Yes |
| Invite member | Send invitation email to attach a User as a `BillingAccountMember` | Yes |
| Member roles | Owner, CoOwner, Member | Yes |
| Deactivate BillingAccount | Soft-disable without deleting history | Yes |
| Merge BillingAccounts | Handle duplicate accounts | No |

---

## Vessel Records

**Goal:** Track per-marina information about vessels (insurance, notes, work-order linkage).

| Feature | Description | MVP |
| --- | --- | --- |
| Create ghost vessel | Add a `Vessel` for a customer not on the platform yet; triggers email-based claim flow | Yes |
| Vessel claim notification | Marina sees when a customer claims their ghost vessel | Yes |
| MarinaVesselRecord | Per-marina overlay (insurance, notes) on the canonical Vessel | Yes |
| Insurance verification | Mark insurance as verified by a staff member with a timestamp | Yes |
| Insurance expiry view | List vessels with insurance expiring soon | Yes |
| Insurance expiry alerts | Auto-notify when insurance is approaching expiry | No (post-MVP) |
| Vessel document upload | Attach registration / insurance docs | No (post-MVP) |

---

## Slip Assignments & Leases

**Goal:** Assign slips to customers for defined periods.

| Feature | Description | MVP |
| --- | --- | --- |
| Assign slip | Link a slip to a `BillingAccount` and `Vessel` for a date range | Yes |
| Assignment types | Seasonal, Annual, Monthly, Transient | Yes |
| Rate override | Override the marina's standard rate for a specific assignment | Yes |
| Sublet policy fields | `AllowOwnerSubletWhenAway`, `AllowHolderSublet`, share percentages | Yes |
| End/terminate assignment | Close out an assignment | Yes |
| Slip availability check | Filter slips by vessel dimensions and date range | Yes |
| Conflict detection | Prevent double-booking | Yes |
| Waitlist management | Queue customers for a slip type or specific slip | No |

---

## Marketplace Listings

**Goal:** List slips on the marketplace for transient bookings.

| Feature | Description | MVP |
| --- | --- | --- |
| Create listing | `AvailabilityWindow` for a date range with price and policy | Yes |
| Instant book vs request | Per-window toggle | Yes |
| Pricing | Base/night, weekly discount, monthly discount, cleaning fee, min/max nights | Yes |
| Calendar editor | Drag a date range, set price/policy | Yes |
| Pause / close listing | Temporarily hide a window without deleting it | Yes |
| Photos | Slip and marina photos shown on the listing | Yes |
| Listing analytics | Views, conversion, search ranking | No (post-MVP) |
| Pricing rules engine | Dynamic, day-of-week, holidays | No (post-MVP) |

---

## Reservation Inbox

**Goal:** Manage incoming reservations from the marketplace.

| Feature | Description | MVP |
| --- | --- | --- |
| Reservation list | All reservations (incoming + assigned), filterable by status | Yes |
| Approve / decline | Request-to-book reservations | Yes |
| Cancel | Confirmed reservations (with reason; cancellation policy snapshot recorded) | Yes |
| Mark NoShow | After arrival window with no boater | Yes |
| Reservation detail | Boater info, vessel, dates, total, payment status, notes | Yes |
| Email notifications | On status changes | Yes |
| Override policies | Confirm or cancel outside the normal flow (Owner only) | Yes |
| Auto-decline expired requests | After configurable timeout | No (post-MVP) |

---

## Sublet Management

**Goal:** Coordinate sublets when long-term tenants are away.

| Feature | Description | MVP |
| --- | --- | --- |
| Holder absence visibility | Marina sees "I'm away" entries from leases at the marina | Yes |
| Create owner-sublet listing | When `AllowOwnerSubletWhenAway = true` for an active absence | Yes |
| Track holder-sublet listings | Marina sees windows the holder is creating themselves | Yes |
| Revenue split snapshot | Per the lease's policy fields, frozen on each window | Yes |
| Settlement | MVP: marina manually credits the holder's invoice; Era 2: automatic via Stripe Connect | Yes (manual) |

---

## Billing & Invoicing

**Goal:** Track what customers owe and what has been paid.

| Feature | Description | MVP |
| --- | --- | --- |
| Create invoice | Manual invoice with line items; link to slip assignment or reservation | Yes |
| Invoice statuses | Draft → Sent → Paid / PartiallyPaid / Overdue / Voided | Yes |
| Add line items | Custom line items (slip fees, fuel, service, sublet credits) | Yes |
| Record payment | Manual recording (cash, check, card, etc.) | Yes |
| Partial payments | Multiple payments against one invoice | Yes |
| Void invoice | Cancel with a reason | Yes |
| Overdue detection | Auto-flag past due-date | Yes |
| Late fee application | Manually add a late-fee line item | Yes |
| Late fee automation | Auto-apply configurable late fees | No |
| Send invoice email | Email invoice PDF to customer | No (post-MVP) |
| Recurring invoices | Auto-generate monthly/seasonal | No |
| Stripe Connect | Online payment + payouts | No (Era 2) |
| Tax configuration | Set tax rate(s) per marina | No |
| Invoice PDF | Printable/downloadable invoice | No |

---

## Maintenance & Work Orders

**Goal:** Track work, whether internal or customer-requested.

| Feature | Description | MVP |
| --- | --- | --- |
| Maintenance request inbox | All customer-submitted requests with status | Yes |
| Update request status | Submitted → UnderReview → InProgress → Completed/Declined | Yes |
| Create work order | From scratch or from a customer request | Yes |
| Assign work order | Assign to a staff member | Yes |
| Work order status | Open → InProgress → OnHold → Completed | Yes |
| Schedule work order | Set scheduled date | Yes |
| Add completion notes | Record what was done | Yes |

---

## Announcements

**Goal:** Keep customers and incoming boaters informed.

| Feature | Description | MVP |
| --- | --- | --- |
| Create announcement | Markdown body; audience: Customers / IncomingBoaters / Both | Yes |
| Draft / publish | Save as draft before publishing | Yes |
| Pin announcement | Pinned items appear first | Yes |
| Expire announcement | Auto-hide after a date | Yes |
| Edit / delete | Manage existing posts | Yes |
| Targeted announcements | By dock, slip, or customer segment | No |
| Email blast | Email all customers | No |
| SMS notifications | Text customers | No |

---

## Reporting & Analytics

**Goal:** Visibility into business performance.

| Feature | Description | MVP |
| --- | --- | --- |
| Occupancy summary | Slips occupied vs. available | No (post-MVP) |
| Revenue report | Invoiced and collected by period | No |
| Aging receivables | Outstanding invoices grouped by age | No |
| Customer activity | Recent activity per BillingAccount | No |
| Slip utilization | Occupancy rate over time | No |
| Booking funnel | Search → view → booking conversion | No |

---

## Notes

- All billing and reservation mutations are written to `AuditLog`.
- Pricing logic lives on `AvailabilityWindow` (per-window) and `SlipAssignment.BaseRate` (per-lease) — not on `Slip` itself.
- Marina's tier (`Tenant.SubscriptionTier`) gates feature access via `[RequiresTier]`. Specific assignments are TBD pending pricing-model work.
- A staff member's permissions come from `Membership.Role` at the relevant marina (or the marina's tenant if Tenant-scoped). See [auth-and-permissions.md](../auth-and-permissions.md).
