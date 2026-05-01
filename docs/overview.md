# MyMarina — Project Overview

## Vision

MyMarina is a two-sided marketplace for marina dockage — like Airbnb, for slips. Boaters discover and reserve dockage at participating marinas (and from private slip owners) anywhere they cruise; hosts list their slips and earn revenue from transient and seasonal stays. Marinas also use the platform to run their day-to-day operations: long-term lease management, invoicing, customer relationships, maintenance, announcements, and staff.

The product wraps two value propositions in one platform:

1. **Marketplace for boaters** — find a slip, reserve it, show up.
2. **SaaS for marinas** — run the operation without spreadsheets.

Both sides reinforce each other. Marinas already need management software; making them a marketplace participant adds incremental revenue and exposure. Boaters get a single account that works at every participating marina — no per-marina sign-ups, no separate logins.

**Domain:** mymarina.org
**Trademark status:** USPTO application dead/refused/dismissed — name is clear to use.

---

## Revenue Model — Two Eras

### Era 1 — SaaS subscriptions (MVP, today)

Marinas pay a subscription fee (Free / Pro / Premium tiers) to use the platform. Boater accounts are free. Reservations are facilitated end-to-end (discovery, booking, confirmation), but **payment for reservations happens off-platform** in MVP — the marina invoices the boater using the existing manual invoicing flow, and we take no cut of the booking value.

Tier gating drives feature access (e.g., advanced reporting, marketplace listing, sublet revenue sharing) and is enforced via `[RequiresTier]` attributes on controller actions. Tier definitions are intentionally placeholder; the exact feature-to-tier assignments are TBD pending pricing/feature-model work.

### Era 2 — Transaction fees (post-MVP, after Stripe Connect)

Once payment processing lands, reservation payments flow through the platform: boater pays MyMarina, MyMarina deducts a platform fee, MyMarina pays out to the host (marina or private slip owner). Tier subscriptions may continue alongside, may be reduced, or may be replaced — the call gets made based on market response.

The MVP data model is built to support Era 2 cleanly. `Reservation`, `AvailabilityWindow`, and `Payment` carry payment-routing fields (`PaymentIntentId`, `PlatformFeeAmount`, `PayoutStatus`, `RevenueSplitSnapshot`) from day one — they default to off-platform values until the payment service is wired in.

---

## The Three Personas

### 1. Platform Operators

MyMarina staff who operate the SaaS product itself. Global role (`PlatformOperator` Identity Role).

- Provision tenants and configure subscription tiers
- Review listings and user reports
- Handle access escalations (with full audit trail)
- Monitor system health and platform billing

### 2. Hosts

Anyone who lists a slip on the marketplace. Two host shapes:

**Marina Hosts** — Commercial marinas, yacht clubs, private boating communities. They list slips they own, and they may also list slips owned by individual customers (with the customer's consent, dockominium-style). They use the platform's full management toolkit: docks, slips, customers, invoicing, maintenance, announcements, staff.

**Private Slip Owners** — Individuals who own one or a few slips. Behind the scenes they get a Free-tier `Tenant` and a single-slip `Marina` auto-created on signup; the UX is branded as "Add my dock" or "Add a slip I own," not "create a marina." Two sub-cases:

- **Dockominium**: The slip is physically located inside another marina (the *host marina*). The owner's personal marina (`MarinaType = Dockominium`) holds the slip; `Slip.HostMarinaId` points to the physical-location marina, which has visibility — and may have approval rights — based on a per-slip policy.
- **Private dock**: The slip is on the owner's own waterfront property, not at any other marina. Modeled as a single-slip `MarinaType = PrivateDock` with `Slip.HostMarinaId = null`. Pure peer-to-peer rental.

### 3. Boaters

Anyone who reserves a slip. Every signed-up user is a boater by default. Boaters can:

- Search slips near a destination, filtered by their boat's dimensions and trip dates
- Reserve transient or seasonal slips (request-to-book or instant-book)
- Manage boats on a single global vessel profile that travels with them across marinas
- View invoices and slip details from any marina they're a customer of
- Submit and track maintenance requests
- Mark themselves "away from my slip" so a host marina can sublet during their absence (with revenue share back to the boater)

A single user account does all of these things at once. **There is no role-toggle and no context switch.**

---

## Identity Model — Single Global User

A `User` has no `TenantId`, no `MarinaId`, and no fixed role. The user is a first-class platform citizen.

Permissions are granted via two independent junctions:

| Junction | Grants |
| --- | --- |
| `Membership` (User → Marina or Tenant) | Host-side permissions (Owner, Manager, Staff). Covers all slip-ownership cases — including private docks and dockominiums — because every slip lives in a Marina. |
| `BillingAccountMember` (User → BillingAccount) | Customer-side permissions at a specific marina (Owner, CoOwner, Member) |

A single user can hold any combination of these — be a marina owner here, a customer there, a private-dock owner somewhere else, and a boater everywhere. Sign in once, see everything you have access to. See [auth-and-permissions.md](./auth-and-permissions.md) for the JWT claim shape.

---

## Vessel Model — User-Owned, Marina-Annotated

Boats are **user-scoped**, not marina-scoped. A `Vessel` is a canonical, global record of a boat (make, model, length, beam, draft, etc.) owned by a `User`. The same vessel travels with the user across all the marinas they visit.

A marina that wants to track its own information about a vessel — insurance verification, internal notes, work-order history — does so via a separate `MarinaVesselRecord` keyed on `(MarinaId, VesselId)`. The vessel owner's data and the marina's records are kept distinct.

Marinas can create vessels for customers who aren't on the platform yet ("ghost vessels"). When the customer signs up via an email invitation, the ghost vessel is claimed and linked to their account. See [vessels.md](./vessels.md) for the claim flow (forthcoming).

This model unlocks future features without rework:

- **Boat marketplace** — a vessel can be transferred between users (sale)
- **Vessel maintenance log** — owner-side service history independent of any marina
- **Trip tracking** — tie reservations and movements to a vessel timeline

---

## Slip Ownership — Marina-Pinned

Every `Slip` is pinned to a `Marina`. There is no per-slip ownership polymorphism. Real-world ownership scenarios are encoded by the marina's `MarinaType`:

| Scenario | `Marina.MarinaType` | `Slip.HostMarinaId` |
| --- | --- | --- |
| Marina-owned (default) | `Commercial` | `null` |
| Yacht club / HOA-owned | `YachtClub` / `PrivateCommunity` | `null` |
| Dockominium (individual-owned, at a marina) | `Dockominium` (single-slip personal marina) | the physical-location marina |
| Private dock (individual-owned, no marina) | `PrivateDock` (single-slip personal marina) | `null` |

Private hosts (dockominium and private-dock owners) get a Free-tier Tenant + single-slip Marina auto-created on signup. The UX never says "marina" for them — they see "your dock," "your slip," "your bookings." The tenant/marina abstraction is plumbing.

A dockominium slip's host marina has a configurable approval policy (`HostMarinaPolicy: None | NotifyOnly | RequiresApproval`) that controls how new bookings against the slip flow through host-marina staff.

**All slip permissions resolve through `Membership` at `Slip.MarinaId`.** No separate slip-owner claim, no JWT bloat. See [marketplace.md](./marketplace.md) (forthcoming) for booking flow and [data-model.md](./data-model.md#slip) for the full Slip schema.

---

## Three Sources of Marketplace Availability

A given slip can become bookable through any of:

1. **Owner-direct** — `Slip.MarinaId` lists the slip itself.
2. **Holder sublet** — the holder of a long-term `SlipAssignment` lists their leased slip while away (subject to lease policy; revenue shared back to the slip's marina).
3. **Owner sublet of leased slip** — the slip's marina lists during the holder's known-absence window (subject to lease policy; revenue shared back to the holder — incentivizing them to flag absences honestly).

The lease (`SlipAssignment`) carries policy flags (`AllowOwnerSubletWhenAway`, `AllowHolderSublet`, `OwnerSubletShareToHolder`, `HolderSubletShareToOwner`) negotiated at lease signing.

This three-source model is unique to MyMarina and is the deliberate differentiator vs. existing marina-management software.

---

## MVP Scope

**Boater side:**

- Sign up with email or social login (Google, Apple, Facebook)
- Manage boats (vessels) on a global profile
- Search slips near a location, filtered by vessel dimensions and dates
- Reserve transient or seasonal slips (request-to-book or instant-book)
- View existing relationships at marinas (current slip, invoices, history)
- Submit and track maintenance requests
- Mark yourself "away" to enable owner sublet (where the lease allows)

**Host side (Marina):**

- Marina onboarding (profile, docks, slips)
- Long-term slip assignments with sublet policy flags
- List slips on the marketplace (instant-book or request-to-book)
- Reservation inbox (approve, decline, manage arrivals)
- BillingAccount management (invite by email; ghost vessels for non-platform customers)
- Manual invoice creation and payment recording
- Maintenance request inbox + work orders
- Announcements
- Staff invitations + memberships

**Host side (Private Slip Owner):**

- Add a slip (dockominium or private dock)
- List on the marketplace with pricing and policy
- Approve / decline bookings (or instant-book)
- Track booking history

**Platform-side:**

- Tenant (subscription) provisioning
- Cross-tenant audit log
- User and listing moderation primitives

**Out of MVP** (post-MVP backlog):

- Stripe Connect / online payment for reservations
- Reviews and ratings (high priority — marketplace trust signal)
- In-app messaging (boater ↔ host)
- Insurance verification automation
- Mobile application
- Recurring invoice generation
- Late-fee automation
- Slip map / visual occupancy view
- PostGIS-based geo search
- Boat marketplace (vessel transfer / sale)
- On-premise / self-hosted packaging

---

## Tenant Routing Strategy

Single domain (`app.mymarina.org`). User is resolved from the JWT; their accessible marinas and billing accounts are derived from `Membership` and `BillingAccountMember` records — all carried as JWT claims. Slip ownership is implicit in marina membership (every slip is pinned to a marina; private hosts have their own single-slip marina).

There is **no per-tenant subdomain routing** and **no context switching** in the UI. A user signs in once; the app shows everything they have access to.

A subdomain-routing variant remains possible for white-label deployments later, but it is out of MVP scope.

---

## Target Customers (Initial)

**Hosts:** Small-to-medium marinas. Initial targets are local marinas where we can collect early feedback and iterate quickly. The data model and infrastructure are designed to scale to large commercial operations and to private slip owners without rearchitecting.

**Boaters:** Recreational cruisers in the U.S. East Coast and Great Lakes regions for v1. Geographic expansion follows host adoption.
