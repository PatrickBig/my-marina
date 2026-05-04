# Feature Area: Boaters

Boaters are anyone who searches for and reserves slips. Every signed-up user is a boater by default — no special enrollment, no per-marina sign-up. A single account works at every participating marina, with vessels and reservation history that travel with the user.

> Replaces v0's `marina-customers.md`. The "customer" framing was tied to one-marina-at-a-time; the marketplace pivot makes the user a first-class platform citizen.

---

## Account & Profile

**Goal:** Let users manage their own identity and preferences.

| Feature | Description | MVP |
| --- | --- | --- |
| Register account | Email+password sign-up with email confirmation | Yes |
| Social login | Sign up / sign in via Google, Apple, Facebook | Yes |
| Link providers | Add Google/Apple/Facebook to an existing account | Yes |
| Edit profile | Name, phone, photo, emergency contact | Yes |
| Change password | Self-service password change | Yes |
| Email preferences | Marketing opt-in/opt-out at signup; granular categories later | Yes (basic) |
| Notification preferences | Per-channel (email, SMS) opt-in for booking/maintenance/announcements | No (post-MVP) |
| Multi-marina view | Single dashboard showing relationships at all marinas | Yes |
| Two-factor auth | TOTP / SMS | No (post-MVP) |
| Delete account | Self-service account deletion (GDPR) | Yes |

---

## Boats (Vessels)

**Goal:** Manage the user's canonical fleet.

| Feature | Description | MVP |
| --- | --- | --- |
| List my boats | Show all `Vessel` records owned by the user | Yes |
| Add a boat | Create a `Vessel` (name, make, model, year, dimensions, type, registration) | Yes |
| Edit a boat | Update vessel details | Yes |
| Archive a boat | Soft-delete; retain on historical assignments and reservations | Yes |
| Claim a ghost vessel | Accept a marina-created `Vessel` linked to my email | Yes |
| Photos | Upload boat photos (1–8) | No (post-MVP) |
| Documents | Upload registration / insurance docs | No (post-MVP) |
| Maintenance log | Owner-side service history | No (post-MVP) |
| Trip log | Where my boat has been | No (post-MVP) |
| Transfer ownership | Sale / gift to another user | No (post-MVP) |

---

## Slip Discovery & Search

**Goal:** Find slips for a trip.

| Feature | Description | MVP |
| --- | --- | --- |
| Search by location | Map or text-input near a destination; uses bounding-box geo | Yes |
| Search by dates | Arrival and departure date/time pickers | Yes |
| Vessel-fit filter | Auto-filter to slips that fit my selected boat (length / beam / draft) | Yes |
| Filters | Slip type, electric, water, max price, instant-book only | Yes |
| Sort | Distance (default), price, newest | Yes |
| Slip detail | Photos, dimensions, amenities, host, reviews (post-MVP), price | Yes |
| Save search / alerts | Notify me when matching slips become available | No (post-MVP) |
| Map view | Listings on an interactive map | Yes (basic Leaflet) |
| PostGIS-based search | "Within X nautical miles along navigable water" | No (post-MVP) |

---

## Reservations

**Goal:** Book a slip; manage the booking; cancel if needed.

| Feature | Description | MVP |
| --- | --- | --- |
| Submit reservation | Date range, vessel, optional note to host | Yes |
| Instant book | Auto-confirms when the listing allows | Yes |
| Request to book | Awaits host approval | Yes |
| Host marina approval flow | Awaits host marina approval (dockominium with `RequiresApproval`) | Yes |
| Reservation detail | Status, slip info, host info, total breakdown, cancellation policy | Yes |
| Cancel reservation | Per cancellation policy; records snapshot | Yes |
| Email notifications | On status transitions (submitted, approved, declined, cancelled, reminder) | Yes |
| Reservation history | Past reservations with status | Yes |
| Pre-booking messaging | Ask the host a question before booking | No (post-MVP) |
| Group reservations | Reserve multiple adjacent slips together | No (post-MVP) |
| Pay online | Stripe Connect (Era 2) | No |
| Reviews | Rate the host after a completed stay | No (post-MVP) |

---

## My Slip (Long-Term Lease)

**Goal:** For boaters with seasonal/annual leases — visibility into their berth.

| Feature | Description | MVP |
| --- | --- | --- |
| View current assignment | Slip name, dock, marina, dates, rate, vessel | Yes |
| Slip amenities | Electric, water, dimensions | Yes |
| Assignment history | Past assignments at this marina (and across marinas) | Yes |
| Sublet policies | View what the lease allows (`AllowOwnerSubletWhenAway`, `AllowHolderSublet`, splits) | Yes |
| End of lease alerts | Notify me before my lease ends | No (post-MVP) |

---

## "I'm Away"

**Goal:** Help marinas maximize utilization while you're gone, with revenue share back to you.

| Feature | Description | MVP |
| --- | --- | --- |
| Mark away | Pick a date range during which I won't use my slip | Yes |
| Edit / cancel away | Change dates or cancel before any sublet booking lands | Yes |
| Sublet preference | Choose: (a) marina lists for me, (b) I list it myself, (c) just block from search | Yes |
| Revenue share view | See lease policy: how much I earn from owner-sublets | Yes |
| List a holder-sublet | Create my own `AvailabilityWindow` (when lease allows) | Yes |
| Earnings tracking | See how much I've earned from sublets | Yes (basic) |

---

## Invoices & Payments

**Goal:** Visibility into billing relationships at marinas.

| Feature | Description | MVP |
| --- | --- | --- |
| List invoices | All invoices across all my BillingAccount memberships | Yes |
| Invoice detail | Line items, payment history, balance due | Yes |
| Payment history | All recorded payments | Yes |
| Pay online | Stripe — boater pays MyMarina, MyMarina pays out to host | No (Era 2) |
| Autopay | Set up recurring payment method | No |
| Download invoice | PDF of invoice | No (post-MVP) |
| Notify on overdue | Reminder before due date | No (post-MVP) |

---

## Maintenance Requests

**Goal:** Report problems and track repairs.

| Feature | Description | MVP |
| --- | --- | --- |
| Submit request | Describe an issue; optionally link to a slip, vessel, or active reservation | Yes |
| View status | Submitted → UnderReview → InProgress → Completed/Declined | Yes |
| Request history | All my past requests across marinas | Yes |
| Add comment | Follow-up after submission | No (post-MVP) |
| Status notifications | Email when status changes | Yes |
| Attach photos | Show the problem | No (post-MVP) |

---

## Announcements

**Goal:** Stay informed about marinas you have a relationship with (or upcoming reservations).

| Feature | Description | MVP |
| --- | --- | --- |
| Announcement feed | Across all marinas where I'm a customer or have an upcoming reservation | Yes |
| Announcement detail | Full markdown content | Yes |
| Mark as read | Personal read tracking | No (post-MVP) |
| Filter by marina | View per-marina announcements | Yes |

---

## Multi-Marina Dashboard

**Goal:** Show everything that's mine in one place.

| Feature | Description | MVP |
| --- | --- | --- |
| Upcoming reservations | All confirmed reservations across all marinas | Yes |
| Long-term lease summary | Each active assignment (dock, slip, dates) | Yes |
| Outstanding invoices | Total balance due + per-marina breakdown | Yes |
| Open maintenance requests | All active across all marinas | Yes |
| Recent announcements | Cross-marina feed (5 most recent) | Yes |
| My boats | Quick links to each `Vessel` | Yes |

---

## Notes

- Boaters never see "marina" branding for private hosts — it's "Pat's Dock," "Maria's slip at Big Bay," etc.
- A boater may simultaneously be a host (their own marina, dockominium, or commercial) and a boater. Both surfaces appear on the same dashboard. No context toggle.
- Email confirmation is required before submitting a reservation, accepting a vessel claim, or accepting a billing-account invitation.
- The "I'm away" flow is a deliberate platform differentiator vs. existing marina-management software — see [marketplace.md > Sublet flows](../marketplace.md#sublet-flows).
