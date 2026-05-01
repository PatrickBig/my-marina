# MyMarina — Glossary

Terms used throughout the docs and the product. Where two terms might mean the same thing, the **preferred** term is bolded.

---

## Personas & roles

- **Boater** — Any user with an account who searches for or reserves slips. Default role for every signed-up user; no separate enrollment required.
- **Host** — Anyone who lists a slip for booking. Either a Marina (organization) or a private slip owner (individual).
- **Platform Operator** — MyMarina staff who administer the SaaS itself. Global role granted via Identity Roles.
- **Marina Owner / Manager / Staff** — Roles within a `Marina` membership. See [auth-and-permissions.md](./auth-and-permissions.md) for the role hierarchy.
- **Tenant Owner** — A user with an Owner-role `Membership` at `Tenant` scope. Has Owner-equivalent access to all marinas under that tenant.
- **Slip Owner** — A user who owns the marina that contains a slip. Resolved via Owner `Membership` at `Slip.MarinaId`. For private-host owners (dockominium / private dock), the marina is their auto-created personal marina (`MarinaType = Dockominium` or `PrivateDock`). The platform does not store slip ownership separately from marina ownership.

## Vessels

- **Vessel** — Internal entity name for any watercraft (boat, jet ski, dinghy, yacht). Used in code, DTOs, JSON, and database tables.
- **Boat** — User-facing label for a Vessel. Used in UI copy, marketing site, emails. Same thing as a Vessel.
- **Ghost vessel** — A `Vessel` with `OwnerUserId = null`, created by a marina for a customer who isn't on the platform yet. Becomes claimed once the owner signs up via an email invitation.
- **MarinaVesselRecord** — A marina's record about a vessel (insurance, internal notes, work-order links). Separate entity from the canonical `Vessel` and never visible to the vessel owner.
- **Claim (vessel)** — The act of a user linking a ghost vessel to their account, typically via an email invitation. Distinct from JWT claims.

## Slips & docks

- **Slip** — A single berth, mooring, anchorage, or dry-storage spot. Always pinned to a `Marina` (private-host slips live in an auto-created single-slip personal marina).
- **Dock** — A named section of a marina containing slips (e.g., "Dock A").
- **Mooring** — A slip with no parent dock (typically a ball or buoy). Modeled as `Slip.DockId = null` with `SlipType = Mooring`.
- **Dockominium** — A slip owned by an individual but physically located at another marina. Modeled as a single-slip `Marina` (`MarinaType = Dockominium`) owned by the individual, with `Slip.HostMarinaId` pointing to the physical-location marina.
- **Private dock** — A slip on the owner's own waterfront property, not at any other marina. Modeled as a single-slip `Marina` (`MarinaType = PrivateDock`) owned by the individual; `Slip.HostMarinaId` is null.
- **Host marina** — For dockominium slips, the marina that physically hosts the slip. May or may not have approval rights over bookings, per `Slip.HostMarinaPolicy`.
- **HostMarinaPolicy** — The host marina's role in a dockominium slip's bookings: `None` (bypassed entirely), `NotifyOnly` (informational), `RequiresApproval` (gate every booking).
- **MarinaType** — `Commercial`, `YachtClub`, `PrivateCommunity`, `Dockominium`, `PrivateDock`. The latter two are auto-created single-slip marinas for individual host owners; users never see the word "marina" for those.

## Reservations & assignments

- **Reservation** — A boater's booking against an `AvailabilityWindow`. Always short-to-medium term and user-initiated.
- **Booking** — *Avoid this term in code and docs.* Use **Reservation**.
- **SlipAssignment** — A long-term lease on a slip (Seasonal, Annual, Monthly, Transient). Operator- or owner-initiated; not a marketplace transaction.
- **AvailabilityWindow** — A period when a slip is bookable on the marketplace. Has a price, listing source (`Owner` / `Holder` / `OwnerForHolder`), and optional instant-book flag.
- **Instant Book** — A reservation that confirms automatically without host approval.
- **Request to Book** — A reservation that requires host approval before confirming.
- **Sublet** — A short-term reservation against a slip that already has a long-term assignment, where the boater is renting from someone other than the slip's outright owner. Two flavors:
  - **Owner sublet** — Owner-initiated when the long-term holder is away (revenue shared with holder).
  - **Holder sublet** — Long-term holder lists the slip themselves (revenue shared with owner per lease).
- **"I'm away"** — Boater self-service flow for marking a date range during which they're not using their slip, enabling owner sublet (where the lease allows).

## Billing & customer relationships

- **BillingAccount** — Marina-side billing entity. Represents a marina's record of a customer relationship. Replaces v0's `CustomerAccount`.
- **BillingAccountMember** — Junction linking a User to a BillingAccount with a role (Owner, CoOwner, Member).
- **Tenant** — SaaS-billing entity. A marina business that pays for the platform. Owns one or more Marinas. Carries the SubscriptionTier.
- **Subscription Tier** — `Free`, `Pro`, `Premium`. Gates feature access via `[RequiresTier]`. Specific assignments are TBD pending pricing model.
- **Membership** — Junction linking a User to a Marina or Tenant with a role (Owner, Manager, Staff). Source of host-side permissions.

## Platform & money

- **Platform Fee** — The cut MyMarina takes from a reservation. Zero in MVP (off-platform payment); set in Era 2.
- **Off-platform payment** — A reservation paid for outside MyMarina (boater pays marina directly via marina-issued invoice). MVP default.
- **Revenue Split** — The breakdown of who gets paid what from a reservation. Stored as a JSON list on `AvailabilityWindow` and snapshotted onto `Reservation` at booking time.
- **Payout** — A transfer of reservation revenue from the platform to a host. Post-MVP, via Stripe Connect.
- **Era 1 / Era 2** — The two revenue eras. Era 1 = SaaS subscriptions only (MVP); Era 2 = transaction fees with Stripe Connect (post-MVP). See [overview.md](./overview.md#revenue-model--two-eras).

## Authorization

- **Claim (JWT)** — A piece of data embedded in the access token. Sources permissions; not the same as a vessel claim.
- **Policy** — An ASP.NET Core authorization policy. Resolved via custom handlers that read JWT claims.
- **Permission rotation** — Forced re-login after a permission change, to refresh JWT claims. See [auth-and-permissions.md](./auth-and-permissions.md#refresh-tokens--permission-rotation).
- **Identity Role** — A global role on `ApplicationUser`. The only one in MVP is `PlatformOperator`. All other authorization comes from junctions.

## Architecture

- **Modular monolith** — Single deployable unit, internally structured as bounded modules with clear seams. Modules communicate via `IMessageBus`.
- **`IMessageBus`** — Application-layer abstraction over background work and inter-module events. MVP impl is Hangfire-backed; future impl is NATS JetStream.
- **`IUserContext`** — Per-request abstraction giving handlers access to the caller's identity and resolved permissions. Replaces v0's `ITenantContext` / `IMarinaContext` / `ICustomerContext`.

---

## Deprecated terms (v0 → new)

These names appear in the v0 codebase and are being **retired** in the overhaul. Do not use them in new code or docs.

| v0 term | New term |
| --- | --- |
| `CustomerAccount` | `BillingAccount` |
| `CustomerAccountMember` | `BillingAccountMember` |
| `Customer` (the user-side concept) | `Boater` (user-facing) / `BillingAccountMember` (billing-side) |
| `UserContext` (the multi-context-switch entity) | replaced by `Membership` + JWT claims; no context switching |
| `ITenantContext` / `IMarinaContext` / `ICustomerContext` | `IUserContext` (single, with claim-derived permissions) |
| `Boat` (entity) | `Vessel` (entity); "Boat" remains as user-facing label |
| `MarinaOwner` / `MarinaStaff` (Identity roles) | `Membership.Role` (Owner, Manager, Staff) |
| `Slip.OwnerKind` / `Slip.OwnerUserId` / `Slip.OwnerMarinaId` | dropped — every `Slip` is pinned to a `Marina` (`Slip.MarinaId`); private hosts get a single-slip personal marina |
