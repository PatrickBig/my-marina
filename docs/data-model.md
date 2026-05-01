# MyMarina — Data Model

> Cross-references: [overview.md](./overview.md) for vision, [auth-and-permissions.md](./auth-and-permissions.md) for the authorization model, [glossary.md](./glossary.md) for terminology.

## Primary Key Strategy

All entities use **UUID v7** (`Guid.CreateVersion7()`, available in .NET 9+) as primary keys. UUID v7 is time-ordered and monotonically increasing, giving B-tree index performance comparable to `int` identity while retaining global uniqueness — no sequence generator, no inter-pod coordination needed. Sequential insertion order eliminates the page-split fragmentation that makes random UUID v4 a poor choice for high-write tables.

---

## Entity Relationship Overview

```text
User  (global identity — no TenantId)
  ├── Membership []           → Marina | Tenant
  ├── BillingAccountMember [] → BillingAccount
  ├── Vessel []               (OwnerUserId)
  └── Reservation []          (BoaterUserId)

Tenant  (SaaS billing — also covers private host owners on Free tier)
  └── Marina []  (MarinaType ∈ Commercial, YachtClub, PrivateCommunity, Dockominium, PrivateDock)
        ├── Dock []
        │     └── Slip []
        ├── Slip []                         (no Dock — moorings, anchorages, private docks)
        ├── BillingAccount []
        │     ├── BillingAccountMember [] → User
        │     └── Invoice []
        │           ├── InvoiceLineItem []
        │           └── Payment []
        ├── MarinaVesselRecord []          → Vessel
        ├── Announcement []
        ├── OperatingExpense []
        ├── MaintenanceRequest []
        │     └── WorkOrder
        └── (staff via Membership)

Slip  (always pinned to a Marina; HostMarinaId optional for dockominiums)
  ├── SlipAssignment []         (long-term lease)
  └── AvailabilityWindow []     (marketplace listings)
        └── Reservation []

Vessel  (canonical, user-owned)
  ├── MarinaVesselRecord []     (per-marina notes / insurance)
  ├── SlipAssignment []         (assigned vessel)
  └── Reservation []            (vessel for the booking)
```

---

## Identity & Authorization

User identity is global. `User` has no `TenantId`, no `MarinaId`, and no fixed role. Permissions are granted via separate junction records and embedded into the JWT as claims at sign-in.

### ApplicationUser

ASP.NET Core Identity `IdentityUser<Guid>`-derived entity. Lives in `MyMarina.Infrastructure.Identity`.

| Field | Type | Notes |
| --- | --- | --- |
| Id | UUID v7 | PK |
| Email | string | Unique |
| EmailConfirmed | bool | Required before reserving / accepting invitations |
| PasswordHash | string? | Null if user only signs in via social provider |
| FirstName | string | |
| LastName | string | |
| PhoneNumber | string? | |
| PhoneNumberConfirmed | bool | |
| ProfilePhotoUrl | string? | |
| MarketingOptIn | bool | |
| TermsAcceptedAt | DateTimeOffset? | |
| IsActive | bool | Soft-disable for moderation |
| LastLoginAt | DateTimeOffset? | |
| CreatedAt | DateTimeOffset | |

Identity-managed tables (`AspNetUserLogins`, `AspNetUserClaims`, `AspNetUserTokens`, etc.) handle external provider logins (Google, Apple, Facebook) and auth tokens.

### IdentityRole (global)

| Role | Purpose |
| --- | --- |
| `PlatformOperator` | MyMarina staff. Bypasses all tenant/marina filters with audit logging. |

That is the **only** global role. All other authorization is junction-based.

### Membership

User's host-side relationship with a Marina or Tenant.

| Field | Type | Notes |
| --- | --- | --- |
| Id | UUID v7 | PK |
| UserId | UUID v7 | FK → ApplicationUser |
| Scope | enum | `Marina`, `Tenant` |
| MarinaId | UUID v7? | Set when `Scope = Marina` |
| TenantId | UUID v7? | Set when `Scope = Tenant` |
| Role | enum | `Owner`, `Manager`, `Staff` |
| InvitedAt | DateTimeOffset | |
| AcceptedAt | DateTimeOffset? | Null until invitation is accepted |
| InvitedByUserId | UUID v7? | Who sent the invite |

A `Tenant`-scoped Owner membership grants access to all marinas under that tenant — useful for marina chains and corporate ownership without per-marina enrollment. A `Marina`-scoped membership grants access only to that marina.

| Role | Capabilities |
| --- | --- |
| Owner | Full control of the marina/tenant; can invite Managers and Staff; can change subscription tier (Tenant scope only). |
| Manager | Full operational control; cannot invite other Owners or change subscription tier. |
| Staff | Day-to-day operations (slip assignments, invoicing, maintenance); cannot invite users or modify marina profile. |

### BillingAccountMember

User's customer-side relationship with a marina's `BillingAccount`.

| Field | Type | Notes |
| --- | --- | --- |
| Id | UUID v7 | PK |
| BillingAccountId | UUID v7 | FK → BillingAccount |
| UserId | UUID v7 | FK → ApplicationUser |
| Role | enum | `Owner`, `CoOwner`, `Member` |
| InvitedAt | DateTimeOffset | |
| AcceptedAt | DateTimeOffset? | |
| InvitedByUserId | UUID v7? | Marina staff who created the invite |

A `BillingAccount` may have zero linked members (a purely marina-managed customer who isn't on the platform). Once at least one user accepts, they become the platform-side contact for that account.

| Role | Capabilities |
| --- | --- |
| Owner | Primary contact; receives billing communications; can invite CoOwners and Members; can edit account profile. |
| CoOwner | Same as Owner — useful for joint ownership (LLC partners, family). |
| Member | Read-only portal access (view invoices, announcements, slip status). |

### Authorization model (overview)

Authorization is **permission-derived row-level access**, not a single tenant filter. EF Core global query filters are still used — but the predicate consults the current user's claims (memberships, billing-account memberships, slip ownership, platform role) rather than a uniform `TenantId` match.

For example, a query against `Slip` returns:

- Public/listed slips (for marketplace search), plus
- Slips owned by the current user, plus
- Slips at marinas where the user has a `Membership` (Marina or Tenant scope), plus
- Slips linked through a `BillingAccountMember` relationship (so customers see their assigned slip)

Platform operators bypass all filters with full audit logging.

See [auth-and-permissions.md](./auth-and-permissions.md) for the JWT claim shape and policy implementation.

---

## Tenant

Top-level **SaaS billing** entity. A Tenant is a marina business — solo marina, marina chain, yacht club, private boating community. The subscription tier lives here.

| Field | Type | Notes |
| --- | --- | --- |
| Id | UUID v7 | PK |
| Name | string | Display name (e.g., "Sunseeker Marina Group") |
| Slug | string | URL-safe, unique |
| SubscriptionTier | enum | `Free`, `Pro`, `Premium` |
| BillingEmail | string | Where SaaS invoices go |
| IsActive | bool | Platform operator can suspend |
| SuspendedAt | DateTimeOffset? | |
| CreatedAt | DateTimeOffset | |

A Tenant owns one or more `Marina` records. Tier is Tenant-wide, not per-marina.

**Every host on the platform** has a Tenant — commercial marina, yacht club, private boating community, dockominium owner, private-dock owner. Private hosts default to the Free tier and typically have a Tenant containing a single Marina with a single Slip. The "Add my dock" / "Add a slip I own" UX flows create the Tenant + Marina + Slip behind the scenes; users never see marina-language for the private case.

---

## Marina

A physical marina facility. Always belongs to a Tenant.

| Field | Type | Notes |
| --- | --- | --- |
| Id | UUID v7 | PK |
| TenantId | UUID v7 | FK → Tenant |
| Name | string | |
| Slug | string | URL-safe; unique within tenant |
| Address | value object | Street, City, State, Zip, Country |
| Latitude | decimal | Required; used for marketplace search |
| Longitude | decimal | Required; used for marketplace search |
| PhoneNumber | string | |
| Email | string | Public contact email |
| Website | string? | |
| Description | string? | Public-facing |
| TimeZoneId | string | IANA timezone |
| MarinaType | enum | `Commercial`, `YachtClub`, `PrivateCommunity`, `Dockominium`, `PrivateDock` |
| IsListed | bool | Whether the marina appears in marketplace search at all |
| CreatedAt | DateTimeOffset | |

---

## Dock

A named section of a marina containing slips.

| Field | Type | Notes |
| --- | --- | --- |
| Id | UUID v7 | PK |
| MarinaId | UUID v7 | FK → Marina |
| Name | string | e.g., "Dock A", "North Dock" |
| Description | string? | |
| SortOrder | int | |

---

## Slip

An individual berth, mooring, anchorage, or dry-storage spot. Always belongs to a Marina (no polymorphic ownership).

| Field | Type | Notes |
| --- | --- | --- |
| Id | UUID v7 | PK |
| MarinaId | UUID v7 | FK → Marina — the marina that owns/operates the slip |
| HostMarinaId | UUID v7? | Physical-location marina when different from `MarinaId` (dockominium case). Null = same as `MarinaId`. |
| HostMarinaPolicy | enum | `None`, `NotifyOnly`, `RequiresApproval` — only meaningful when `HostMarinaId` is set |
| DockId | UUID v7? | Under a dock at `MarinaId`; null for moorings, anchorages, private docks |
| Name | string | "A-12", "Mooring 3", "Lakeshore Dock" |
| SlipType | enum | `Floating`, `Fixed`, `Mooring`, `DryStorage`, `Anchorage` |
| MaxLength | decimal | Feet |
| MaxBeam | decimal | Feet |
| MaxDraft | decimal | Feet |
| HasElectric | bool | |
| Electric | enum? | `Amp30`, `Amp50`, `Amp100` |
| HasWater | bool | |
| Status | enum | `Active`, `UnderMaintenance`, `Inactive` |
| Latitude | decimal | Always set (for marketplace search) |
| Longitude | decimal | Always set |
| Address | value object? | For private docks; null when slip inherits from `MarinaId`'s address |
| Notes | string? | Owner-facing description |

### Ownership model — marina-pinned

Every slip is owned by a `Marina`. There is no per-slip ownership polymorphism. Real-world ownership scenarios are encoded via `Marina.MarinaType`:

| Real-world scenario | How it's modeled |
| --- | --- |
| Marina-owned (default) | Slip's `MarinaId` is a normal commercial marina. `HostMarinaId` is null. |
| Yacht club / HOA-owned | `MarinaId`'s `MarinaType = YachtClub` or `PrivateCommunity`. `HostMarinaId` null. |
| Private dock at owner's home | `MarinaId`'s `MarinaType = PrivateDock` — auto-created when the user adds their dock; the user is the marina's Owner. `HostMarinaId` null. |
| Dockominium (individual-owned slip at a real marina) | `MarinaId`'s `MarinaType = Dockominium` — a single-slip marina owned by the individual. `HostMarinaId` points to the physical-location marina. |

**Slip permissions resolve through `Membership` at `Slip.MarinaId`.** There is no separate slip-ownership claim. Dockominium-host visibility/approval flows through `Membership` at `Slip.HostMarinaId` plus the `HostMarinaPolicy`.

### `HostMarinaPolicy` semantics

Only meaningful when `HostMarinaId` is set (dockominium case).

| Policy | Effect on bookings against this slip |
| --- | --- |
| `None` | Host marina is bypassed entirely. The slip's `MarinaId` (the dockominium owner) controls everything. |
| `NotifyOnly` (default for new dockominiums) | Host marina sees the booking on their dashboard and is notified, but does not gate it. |
| `RequiresApproval` | Host marina must approve every booking before it confirms (in addition to the owner's approval, if request-to-book). |

### Pricing

Pricing lives on `AvailabilityWindow`, **not** on `Slip`. The same slip may be priced differently across windows (peak season, off-season, owner-direct vs. sublet). The slip carries no rate fields.

### Three sources of marketplace availability

A slip becomes bookable only via an `AvailabilityWindow`. Three sources, distinguished by `AvailabilityWindow.ListedByKind`:

1. **Owner-direct** — `Slip.MarinaId` lists the slip.
2. **Holder sublet** — the current `SlipAssignment` holder lists their leased slip while away (subject to `SlipAssignment.AllowHolderSublet`; revenue split with the slip's marina per lease terms).
3. **Owner sublet of leased slip** — `Slip.MarinaId` lists during the holder's absence (subject to `SlipAssignment.AllowOwnerSubletWhenAway`; revenue split back to the holder per the lease).

---

## SlipAssignment

A long-term lease on a slip. Links a slip to a `BillingAccount` (the marina's customer record) and a specific `Vessel`.

| Field | Type | Notes |
| --- | --- | --- |
| Id | UUID v7 | PK |
| SlipId | UUID v7 | FK → Slip |
| BillingAccountId | UUID v7 | FK → BillingAccount — the holder's billing entity at the slip's marina |
| VesselId | UUID v7 | FK → Vessel — the assigned boat |
| AssignmentType | enum | `Seasonal`, `Annual`, `Monthly`, `Transient` |
| StartDate | DateOnly | |
| EndDate | DateOnly? | Null = open-ended |
| BaseRate | decimal | The rate the holder is charged |
| AllowOwnerSubletWhenAway | bool | Owner may sublet when the holder is "away" |
| AllowHolderSublet | bool | Holder may sublet themselves |
| OwnerSubletShareToHolder | decimal | Fraction (0–1) of owner-sublet revenue paid to holder |
| HolderSubletShareToOwner | decimal | Fraction (0–1) of holder-sublet revenue paid to owner |
| Notes | string? | |
| CreatedAt | DateTimeOffset | |

A walk-up customer with no existing relationship still gets a `BillingAccount` created on the fly (one click for the marina), keeping the model uniform.

The sublet policy fields are negotiated at lease signing and snapshotted onto each `AvailabilityWindow` and `Reservation` derived from this assignment, so changes mid-season don't retroactively alter existing bookings.

### "Away" flow

Holders mark themselves away with a date range. The flow:

1. Holder taps **"I'll be away"** in the app, picks `start`/`end`.
2. If `AllowOwnerSubletWhenAway = true`: the system surfaces this absence to `Slip.MarinaId`'s dashboard. Marina staff may create an `AvailabilityWindow(ListedByKind = OwnerForHolder, RelatedAssignmentId = <this>)` with `RevenueSplit` derived from `OwnerSubletShareToHolder`.
3. If `AllowOwnerSubletWhenAway = false`: the absence is recorded as informational only (security/yard tracking). No window is created.
4. Independent of the absence flag, if `AllowHolderSublet = true`, the holder may directly create their own `AvailabilityWindow(ListedByKind = Holder)` at any time.

---

## AvailabilityWindow

A period when a slip is bookable on the marketplace. Carries pricing, listing source, and revenue-split rules.

| Field | Type | Notes |
| --- | --- | --- |
| Id | UUID v7 | PK |
| SlipId | UUID v7 | FK → Slip |
| ListedByKind | enum | `Owner`, `Holder`, `OwnerForHolder` |
| ListedByMarinaId | UUID v7? | Set when `ListedByKind` is `Owner` or `OwnerForHolder` (always = `Slip.MarinaId`) |
| ListedByBillingAccountId | UUID v7? | Set when `ListedByKind` is `Holder` (= the holder's `SlipAssignment.BillingAccountId`) |
| RelatedAssignmentId | UUID v7? | Non-null for `Holder` and `OwnerForHolder` kinds |
| StartsAt | DateTimeOffset | |
| EndsAt | DateTimeOffset | |
| InstantBook | bool | True = boater can book without approval |
| MinNights | int? | |
| MaxNights | int? | |
| BasePricePerNight | decimal | |
| WeeklyDiscount | decimal? | Fraction (0–1) applied for stays ≥ 7 nights |
| MonthlyDiscount | decimal? | Fraction (0–1) applied for stays ≥ 28 nights |
| CleaningFee | decimal? | Optional flat fee added to total |
| RevenueSplit | jsonb | Array of `{ payeeKind, payeeId, percent }` |
| Status | enum | `Open`, `Paused`, `FullyBooked`, `Closed` |
| CreatedAt | DateTimeOffset | |

A slip can have multiple non-overlapping windows over time. Application logic enforces non-overlap at write time. A booking against a window automatically reduces availability; if a window is fully consumed, its `Status` becomes `FullyBooked`.

### `RevenueSplit` shape

```json
[
  { "payeeKind": "SlipOwner",  "payeeId": "01HX...marinaId",         "percent": 0.85 },
  { "payeeKind": "Holder",     "payeeId": "01HX...billingAccountId", "percent": 0.10 },
  { "payeeKind": "Platform",   "payeeId": null,                      "percent": 0.05 }
]
```

`payeeKind` ∈ { `SlipOwner`, `Holder`, `HostMarina`, `Platform` }. `payeeId` is a Marina ID for `SlipOwner` and `HostMarina`, a BillingAccount ID for `Holder`, and null for `Platform`. Sum of `percent` must equal 1.0. In MVP the Platform percent is 0 (off-platform payment); reserved for Era 2.

---

## Reservation

A boater's booking against an `AvailabilityWindow`.

| Field | Type | Notes |
| --- | --- | --- |
| Id | UUID v7 | PK |
| BoaterUserId | UUID v7 | FK → ApplicationUser — who reserved |
| VesselId | UUID v7 | FK → Vessel — the boat being brought |
| SlipId | UUID v7 | FK → Slip |
| AvailabilityWindowId | UUID v7 | FK → AvailabilityWindow |
| ArrivesAt | DateTimeOffset | |
| DepartsAt | DateTimeOffset | |
| Status | enum | `PendingApproval`, `PendingHostMarinaApproval`, `Confirmed`, `Declined`, `Cancelled`, `Completed`, `NoShow` |
| BasePrice | decimal | |
| Fees | decimal | |
| Taxes | decimal | |
| Total | decimal | |
| RevenueSplitSnapshot | jsonb | Frozen copy of `AvailabilityWindow.RevenueSplit` at booking time |
| CancellationPolicySnapshot | jsonb | Frozen at booking |
| PaymentIntentId | string? | Reserved for Stripe Connect |
| PaymentStatus | enum | `OffPlatform` (MVP default), `Pending`, `Captured`, `Refunded` |
| PlatformFeeAmount | decimal | Computed; zero in MVP |
| RequestedAt | DateTimeOffset | |
| ConfirmedAt | DateTimeOffset? | |
| DeclinedAt | DateTimeOffset? | |
| CancelledAt | DateTimeOffset? | |
| CancelledByUserId | UUID v7? | |
| Notes | string? | Boater note to host |

### Reservation status transitions

| From | To | Trigger |
| --- | --- | --- |
| (none) | `Confirmed` | Created against an `InstantBook=true` window with no host-marina approval needed |
| (none) | `PendingApproval` | Created against an `InstantBook=false` window |
| (none) | `PendingHostMarinaApproval` | Created against a slip with `HostMarinaPolicy = RequiresApproval` |
| `PendingApproval` | `Confirmed` | Owner approved |
| `PendingApproval` | `Declined` | Owner declined |
| `PendingHostMarinaApproval` | `PendingApproval` | Host marina approved (still needs owner approval if not InstantBook) |
| `PendingHostMarinaApproval` | `Confirmed` | Host marina approved AND window is InstantBook |
| `PendingHostMarinaApproval` | `Declined` | Host marina declined |
| `Confirmed` | `Cancelled` | Boater or host cancelled before arrival |
| `Confirmed` | `Completed` | Auto-set after `DepartsAt` passes without issue |
| `Confirmed` | `NoShow` | Host marks no-show after `ArrivesAt` window |

---

## Vessel

The canonical record of a boat. User-owned. Travels with the user across all marinas.

| Field | Type | Notes |
| --- | --- | --- |
| Id | UUID v7 | PK |
| OwnerUserId | UUID v7? | Null = unclaimed (marina-created ghost vessel) |
| ClaimEmail | string? | For ghost vessels — email the marina entered for the owner |
| ClaimedAt | DateTimeOffset? | When ownership was assigned |
| Name | string | Vessel name |
| Make | string? | |
| Model | string? | |
| Year | int? | |
| Length | decimal | Feet |
| Beam | decimal | Feet |
| Draft | decimal | Feet |
| BoatType | enum | `Sailboat`, `Powerboat`, `Catamaran`, `Dinghy`, `PWC`, `Other` |
| HullColor | string? | |
| RegistrationNumber | string? | |
| RegistrationState | string? | |
| IsArchived | bool | Soft-deleted by user; retains historical references |
| CreatedAt | DateTimeOffset | |

Vessel ownership transfer (boat sale) is post-MVP. Historical references (`SlipAssignment.VesselId`, `Reservation.VesselId`, `MarinaVesselRecord.VesselId`) remain stable across transfers — the `OwnerUserId` field is updated, not the `Id`.

### Ghost vessel claim flow

1. Marina staff create a `BillingAccount` for a customer who isn't on the platform yet. They enter the customer's email and add a vessel.
2. A `Vessel` is created with `OwnerUserId = null`, `ClaimEmail = <entered email>`.
3. An invitation email is sent with a claim link.
4. When the customer signs up (or signs in if already registered) using the claim link, the matching ghost vessel is linked: `OwnerUserId` set, `ClaimedAt` recorded.
5. The marina's `MarinaVesselRecord` (insurance, notes) remains untouched — it links to the same `VesselId`.

If a marina creates a ghost vessel for an email that already has a registered user, the claim is offered automatically on next sign-in.

See [vessels.md](./vessels.md) (forthcoming) for the full claim UX.

---

## MarinaVesselRecord

Marina-specific information about a `Vessel`. One record per `(MarinaId, VesselId)` pair.

| Field | Type | Notes |
| --- | --- | --- |
| Id | UUID v7 | PK |
| MarinaId | UUID v7 | FK → Marina |
| VesselId | UUID v7 | FK → Vessel |
| BillingAccountId | UUID v7? | Marina's billing entity for this vessel |
| InsuranceProvider | string? | |
| InsurancePolicyNumber | string? | |
| InsuranceExpiresOn | DateOnly? | |
| InsuranceVerifiedAt | DateTimeOffset? | When marina staff confirmed coverage |
| InsuranceVerifiedByUserId | UUID v7? | |
| Notes | string? | Marina-only — never visible to vessel owner |
| CreatedAt | DateTimeOffset | |

The vessel owner sees their canonical `Vessel` data; they cannot see `MarinaVesselRecord.Notes` or other marina-internal fields. Future: a configurable visibility flag on `InsuranceExpiresOn` so the owner can be reminded.

---

## BillingAccount

Marina-side billing entity. Replaces v0's `CustomerAccount`. Represents the marina's record of a customer relationship; may or may not be linked to platform users.

| Field | Type | Notes |
| --- | --- | --- |
| Id | UUID v7 | PK |
| MarinaId | UUID v7 | FK → Marina |
| DisplayName | string | Account name (e.g., "Smith Family", "Blue Water Charters LLC") |
| BillingEmail | string | Primary billing contact email |
| BillingPhone | string? | |
| BillingAddress | value object? | Street, City, State, Zip, Country |
| EmergencyContactName | string? | |
| EmergencyContactPhone | string? | |
| Notes | string? | Marina-only |
| IsActive | bool | Soft-disable |
| CreatedAt | DateTimeOffset | |

A `BillingAccount` may have zero linked members (purely marina-managed customer who isn't on the platform). It retains all marina-side data even before any user is linked.

---

## Invoice

| Field | Type | Notes |
| --- | --- | --- |
| Id | UUID v7 | PK |
| MarinaId | UUID v7 | FK → Marina |
| BillingAccountId | UUID v7 | FK → BillingAccount |
| ReservationId | UUID v7? | Optional link if the invoice covers a marketplace reservation |
| SlipAssignmentId | UUID v7? | Optional link if the invoice covers a long-term lease period |
| InvoiceNumber | string | Sequential per marina (not per tenant) |
| Status | enum | `Draft`, `Sent`, `PartiallyPaid`, `Paid`, `Overdue`, `Voided` |
| IssuedDate | DateOnly | |
| DueDate | DateOnly | |
| SubTotal | decimal | |
| TaxAmount | decimal | |
| TotalAmount | decimal | |
| AmountPaid | decimal | |
| BalanceDue | decimal | Computed |
| Notes | string? | |
| CreatedAt | DateTimeOffset | |

---

## InvoiceLineItem

| Field | Type | Notes |
| --- | --- | --- |
| Id | UUID v7 | PK |
| InvoiceId | UUID v7 | FK → Invoice |
| Description | string | |
| Quantity | decimal | |
| UnitPrice | decimal | |
| LineTotal | decimal | |
| SlipAssignmentId | UUID v7? | Optional link for slip-related charges |
| ReservationId | UUID v7? | Optional link for reservation-related charges |

---

## Payment

A payment applied to an invoice. Manual recording in MVP; payment-provider fields reserved for Era 2.

| Field | Type | Notes |
| --- | --- | --- |
| Id | UUID v7 | PK |
| InvoiceId | UUID v7 | FK → Invoice |
| Amount | decimal | |
| PaidOn | DateOnly | |
| Method | enum | `Cash`, `Check`, `CreditCard`, `BankTransfer`, `Other` |
| ReferenceNumber | string? | Check #, transaction ID, etc. |
| Notes | string? | |
| PaymentProviderId | string? | Reserved for Era 2 |
| PaymentProviderReference | string? | External transaction ID |
| RecordedByUserId | UUID v7 | FK → ApplicationUser |
| CreatedAt | DateTimeOffset | |

---

## OperatingExpense

A non-billable cost incurred by the marina (labor, supplies, fuel). Used for cost tracking and profitability analysis.

| Field | Type | Notes |
| --- | --- | --- |
| Id | UUID v7 | PK |
| MarinaId | UUID v7 | FK → Marina |
| Category | string | "Labor", "Supplies", "Fuel", "Utilities", "Maintenance" |
| Description | string | |
| Amount | decimal | |
| IncurredDate | DateOnly | |
| RelatedEntityType | string? | "WorkOrder", "Slip" |
| RelatedEntityId | UUID v7? | |
| RecordedByUserId | UUID v7 | FK → ApplicationUser |
| CreatedAt | DateTimeOffset | |

---

## MaintenanceRequest

A service request submitted by a boater. Boater-centric — submitter is a `User` directly (not gated through `BillingAccount`), so a transient boater can also report issues.

| Field | Type | Notes |
| --- | --- | --- |
| Id | UUID v7 | PK |
| MarinaId | UUID v7 | FK → Marina (where the request was filed) |
| BoaterUserId | UUID v7 | FK → ApplicationUser — who submitted |
| BillingAccountId | UUID v7? | Optional — set when submitter is a member of an account at this marina |
| VesselId | UUID v7? | Optional — the boat the issue relates to |
| SlipId | UUID v7? | Optional — the slip the issue relates to |
| ReservationId | UUID v7? | Optional — set when filed during a reservation stay |
| Title | string | |
| Description | string | |
| Status | enum | `Submitted`, `UnderReview`, `InProgress`, `Completed`, `Declined` |
| Priority | enum | `Low`, `Medium`, `High`, `Urgent` |
| SubmittedAt | DateTimeOffset | |
| ResolvedAt | DateTimeOffset? | |

---

## WorkOrder

The marina's internal work order, optionally linked to a customer maintenance request.

| Field | Type | Notes |
| --- | --- | --- |
| Id | UUID v7 | PK |
| MarinaId | UUID v7 | FK → Marina |
| MaintenanceRequestId | UUID v7? | FK — nullable for purely internal work |
| Title | string | |
| Description | string | |
| AssignedToUserId | UUID v7? | FK → ApplicationUser (staff member) |
| Status | enum | `Open`, `InProgress`, `OnHold`, `Completed`, `Cancelled` |
| Priority | enum | `Low`, `Medium`, `High`, `Urgent` |
| ScheduledDate | DateOnly? | |
| CompletedAt | DateTimeOffset? | |
| Notes | string? | |
| CreatedAt | DateTimeOffset | |

---

## Announcement

A news/update post from a marina to its customers and incoming boaters.

| Field | Type | Notes |
| --- | --- | --- |
| Id | UUID v7 | PK |
| MarinaId | UUID v7 | FK → Marina |
| Title | string | |
| Body | string | Markdown |
| Audience | enum | `Customers`, `IncomingBoaters`, `Both` |
| PublishedAt | DateTimeOffset? | Null = draft |
| ExpiresAt | DateTimeOffset? | |
| IsPinned | bool | |
| CreatedByUserId | UUID v7 | FK → ApplicationUser |
| CreatedAt | DateTimeOffset | |

`Audience = IncomingBoaters` allows a marina to publish messages that reservation guests see ("Welcome — gate code is 4321"); `Customers` is for long-term billing-account members; `Both` shows to both.

---

## RefreshToken

Server-side rotation and revocation for refresh tokens. See [auth-and-permissions.md](./auth-and-permissions.md).

| Field | Type | Notes |
| --- | --- | --- |
| Id | UUID v7 | PK |
| UserId | UUID v7 | FK → ApplicationUser |
| TokenHash | string | SHA-256 hash of the token (never store raw) |
| ExpiresAt | DateTimeOffset | |
| RevokedAt | DateTimeOffset? | |
| ReplacedByTokenId | UUID v7? | Set on rotation |
| CreatedByIp | string? | |
| CreatedAt | DateTimeOffset | |

---

## AuditLog

Append-only record of all mutations. No deletes, no updates.

| Field | Type | Notes |
| --- | --- | --- |
| Id | UUID v7 | PK |
| UserId | UUID v7? | Who performed the action; null for system actions |
| MarinaId | UUID v7? | Marina-scoped actions; null for cross-marina or platform-operator actions |
| TenantId | UUID v7? | Tenant-scoped actions |
| Action | string | "slip.assigned", "invoice.created", "reservation.confirmed", etc. |
| EntityType | string | "Slip", "Invoice", "Reservation" |
| EntityId | UUID v7 | |
| Before | jsonb? | Previous state (null for creates) |
| After | jsonb? | New state (null for deletes) |
| IpAddress | string? | |
| UserAgent | string? | |
| Timestamp | DateTimeOffset | |

---

## Future Entities (Post-MVP)

These are anticipated but not designed in detail yet. The MVP data model is laid out so these can be added without restructuring existing tables.

- **Review** — boater rates host (and host rates boater) after a completed reservation
- **MessageThread / Message** — in-app messaging between boater and host
- **PayoutAccount** — Stripe Connect linked-account record, one per Marina (covers all host shapes since every host is a Marina)
- **Payout** — recorded payout transaction
- **CancellationPolicyTemplate** — host-defined templates referenced by `AvailabilityWindow`
- **VesselDocument** — uploaded files (registration, insurance docs)
- **VesselMaintenanceLog** — owner-side service history independent of any marina
- **VesselTrip** — owner-side trip log linking reservations and movements
- **VesselTransfer** — boat-marketplace ownership transfer record
- **InsuranceVerification** — automated insurance lookup and verification
- **Waitlist** — slip type / dock waitlist
- **InventoryItem / InventoryTransaction** — fuel, pump-out, supplies
- **NotificationTemplate / NotificationLog** — email/SMS templating and audit
- **FleetVessel / VesselRental** — marina-owned charter fleet
- **SubscriptionPlan / SubscriptionInvoice** — platform-side billing for Tenants
