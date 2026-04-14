# MyMarina — Data Model

## Primary Key Strategy

All entities use **UUID v7** (`Guid.CreateVersion7()`, available in .NET 9+) as primary keys. UUID v7 is time-ordered and monotonically increasing, giving B-tree index performance comparable to `int` identity while retaining global uniqueness — no sequence generator, no inter-pod coordination needed. Sequential insertion order eliminates the page-split fragmentation that makes random UUID v4 a poor choice for high-write tables.

---

## Entity Relationship Overview

```
Tenant  (corporate/billing entity; may own multiple marinas)
  │
  ├── Marina []
  │     ├── Dock []
  │     │     └── Slip []
  │     │           └── SlipAssignment [] ──────────────────┐
  │     │                                                   │
  │     ├── Slip []  (moorings — DockId is null)            │
  │     │     └── SlipAssignment [] ──────────────────────┐ │
  │     │                                                 │ │
  │     ├── Announcement []                               │ │
  │     └── Staff (User with MarinaId set) []             │ │
  │                                                       │ │
  ├── CustomerAccount []  ◄──────────────────────────────── ┘
  │     ├── CustomerAccountMember []  (1+ Users per account)
  │     ├── Boat []
  │     │     └── (linked to SlipAssignment)
  │     ├── Invoice []
  │     │     ├── InvoiceLineItem []
  │     │     └── Payment []
  │     └── MaintenanceRequest []
  │           └── WorkOrder (1:1, created by marina operator)
  │
  └── AuditLog []
```

---

## Tenancy & Marina Scoping

Two levels of data isolation are applied:

1. **Tenant-level** — EF Core global query filters enforce `TenantId = @currentTenantId` on every tenant-scoped entity. No query escapes this filter unless the caller is a platform operator.
2. **Marina-level** — Users may be scoped to a single Marina within a Tenant (staff) or have no marina restriction (corporate operators). A `IMarinaContext` service (alongside `ITenantContext`) provides the currently active `MarinaId`.

| User type | `TenantId` | `MarinaId` | Access |
| --- | --- | --- | --- |
| Platform Operator | null | null | Cross-tenant (bypass filter) |
| Corporate Operator | set | null | All marinas under the Tenant |
| Marina Owner / Staff | set | set | Only their assigned Marina |
| Customer | set | null | Their CustomerAccount data only |

---

## Core Entities

### Tenant

The top-level corporate/billing entity. A single Tenant can own multiple Marinas (e.g., a marina management company). MVP enforces one Marina per Tenant at the business logic layer, not the schema.

| Field | Type | Notes |
| --- | --- | --- |
| Id | UUID v7 | PK |
| Name | string | Display name (e.g., "Sunseeker Marina Group") |
| Slug | string | URL-safe identifier, unique |
| SubscriptionTier | enum | Free, Starter, Pro, Enterprise |
| IsActive | bool | Platform operator can suspend |
| CreatedAt | DateTimeOffset | |

---

### Marina

An individual marina facility. Tenant-scoped.

| Field | Type | Notes |
| --- | --- | --- |
| Id | UUID v7 | PK |
| TenantId | UUID v7 | FK → Tenant |
| Name | string | |
| Address | value object | Street, City, State, Zip, Country |
| PhoneNumber | string | |
| Email | string | |
| Website | string? | |
| Description | string? | Shown to customers |
| TimeZoneId | string | IANA timezone ID (e.g., "America/New_York") |
| CreatedAt | DateTimeOffset | |

---

### Dock

A named section of a marina containing slips.

| Field | Type | Notes |
| --- | --- | --- |
| Id | UUID v7 | PK |
| TenantId | UUID v7 | Global query filter |
| MarinaId | UUID v7 | FK → Marina |
| Name | string | e.g., "Dock A", "North Dock" |
| Description | string? | |
| SortOrder | int | Display ordering |

---

### Slip

An individual boat berth or mooring. `DockId` is nullable — a null `DockId` indicates a free-standing mooring, anchorage, or buoy with no dock parent. `MarinaId` is always set regardless.

| Field | Type | Notes |
| --- | --- | --- |
| Id | UUID v7 | PK |
| TenantId | UUID v7 | Global query filter |
| MarinaId | UUID v7 | FK → Marina (always set) |
| DockId | UUID v7? | FK → Dock; null = free-standing mooring |
| Name | string | e.g., "A-12", "Mooring Ball 3" |
| SlipType | enum | Floating, Fixed, Mooring, DryStorage, Anchorage |
| MaxLength | decimal | Feet |
| MaxBeam | decimal | Feet |
| MaxDraft | decimal | Feet |
| HasElectric | bool | |
| Electric | enum? | Amp30, Amp50, Amp100 |
| HasWater | bool | |
| RateType | enum | PerFoot, Flat |
| DailyRate | decimal? | Transient rate |
| MonthlyRate | decimal? | |
| AnnualRate | decimal? | |
| Status | enum | Available, Occupied, UnderMaintenance, Inactive |
| Latitude | decimal? | GPS coordinate; useful for mooring/anchorage map views |
| Longitude | decimal? | GPS coordinate |
| Notes | string? | |

---

### SlipAssignment

Links a slip to a customer account and boat for a period of time.

| Field | Type | Notes |
| --- | --- | --- |
| Id | UUID v7 | PK |
| TenantId | UUID v7 | Global query filter |
| SlipId | UUID v7 | FK → Slip |
| CustomerAccountId | UUID v7 | FK → CustomerAccount |
| BoatId | UUID v7 | FK → Boat |
| AssignmentType | enum | Seasonal, Annual, Monthly, Transient |
| StartDate | DateOnly | |
| EndDate | DateOnly? | Null = open-ended |
| RateOverride | decimal? | If different from slip's standard rate |
| Notes | string? | |
| CreatedAt | DateTimeOffset | |

---

### User

Authentication and identity record. Shared across all personas. Users can belong to a Tenant and optionally be scoped to a specific Marina.

| Field | Type | Notes |
| --- | --- | --- |
| Id | UUID v7 | PK (ASP.NET Core Identity) |
| Email | string | Unique |
| PasswordHash | string | |
| FirstName | string | |
| LastName | string | |
| PhoneNumber | string? | |
| Role | enum | PlatformOperator, MarinaOwner, MarinaStaff, Customer |
| TenantId | UUID v7? | Null for platform operators |
| MarinaId | UUID v7? | Set for marina-scoped staff; null = access to all marinas in the tenant |
| IsActive | bool | |
| CreatedAt | DateTimeOffset | |
| LastLoginAt | DateTimeOffset? | |

---

### CustomerAccount

The billing and ownership entity for a marina customer. A CustomerAccount may have multiple Users (members) — family members, business partners, charter company staff, etc.

| Field | Type | Notes |
| --- | --- | --- |
| Id | UUID v7 | PK |
| TenantId | UUID v7 | Global query filter |
| DisplayName | string | Account name (e.g., "Smith Family", "Blue Water Charters LLC") |
| BillingEmail | string | Primary billing contact email |
| BillingPhone | string? | |
| BillingAddress | value object? | Street, City, State, Zip, Country |
| EmergencyContactName | string? | |
| EmergencyContactPhone | string? | |
| Notes | string? | Internal marina notes |
| CreatedAt | DateTimeOffset | |

---

### CustomerAccountMember

Links a User to a CustomerAccount with a role. Multiple members may belong to the same account.

| Field | Type | Notes |
| --- | --- | --- |
| Id | UUID v7 | PK |
| TenantId | UUID v7 | Global query filter |
| CustomerAccountId | UUID v7 | FK → CustomerAccount |
| UserId | UUID v7 | FK → User |
| Role | enum | Owner, CoOwner, Member |
| CreatedAt | DateTimeOffset | |

**Roles:**

| Role | Description |
| --- | --- |
| Owner | Primary contact; full account access; receives billing communications |
| CoOwner | Same rights as Owner; useful for joint ownership |
| Member | Read-only portal access (view invoices, announcements, slip status) |

---

### Boat

A vessel registered to a CustomerAccount. A CustomerAccount may have multiple boats.

| Field | Type | Notes |
| --- | --- | --- |
| Id | UUID v7 | PK |
| TenantId | UUID v7 | Global query filter |
| CustomerAccountId | UUID v7 | FK → CustomerAccount |
| Name | string | Vessel name |
| Make | string? | |
| Model | string? | |
| Year | int? | |
| Length | decimal | Feet |
| Beam | decimal | Feet |
| Draft | decimal | Feet |
| BoatType | enum | Sailboat, Powerboat, Catamaran, Dinghy, Other |
| HullColor | string? | |
| RegistrationNumber | string? | |
| RegistrationState | string? | |
| InsuranceProvider | string? | |
| InsurancePolicyNumber | string? | |
| InsuranceExpiresOn | DateOnly? | |

---

### Invoice

A billing record issued to a CustomerAccount.

| Field | Type | Notes |
| --- | --- | --- |
| Id | UUID v7 | PK |
| TenantId | UUID v7 | Global query filter |
| CustomerAccountId | UUID v7 | FK → CustomerAccount |
| InvoiceNumber | string | Human-readable, sequential per tenant |
| Status | enum | Draft, Sent, PartiallyPaid, Paid, Overdue, Voided |
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

### InvoiceLineItem

| Field | Type | Notes |
| --- | --- | --- |
| Id | UUID v7 | PK |
| InvoiceId | UUID v7 | FK → Invoice |
| Description | string | |
| Quantity | decimal | |
| UnitPrice | decimal | |
| LineTotal | decimal | |
| SlipAssignmentId | UUID v7? | Optional link for slip-related charges |

---

### Payment

A payment applied to an invoice. Supports manual recording in MVP; payment provider integration is additive.

| Field | Type | Notes |
| --- | --- | --- |
| Id | UUID v7 | PK |
| TenantId | UUID v7 | Global query filter |
| InvoiceId | UUID v7 | FK → Invoice |
| Amount | decimal | |
| PaidOn | DateOnly | |
| Method | enum | Cash, Check, CreditCard, BankTransfer, Other |
| ReferenceNumber | string? | Check #, transaction ID, etc. |
| Notes | string? | |
| PaymentProviderId | string? | Reserved for future payment processor integration |
| PaymentProviderReference | string? | External transaction ID |
| RecordedByUserId | UUID v7 | FK → User |
| CreatedAt | DateTimeOffset | |

---

### MaintenanceRequest

A service request submitted by a customer member.

| Field | Type | Notes |
| --- | --- | --- |
| Id | UUID v7 | PK |
| TenantId | UUID v7 | Global query filter |
| CustomerAccountId | UUID v7 | FK → CustomerAccount |
| SlipId | UUID v7? | Optional — the slip the issue relates to |
| BoatId | UUID v7? | Optional — the boat the issue relates to |
| Title | string | |
| Description | string | |
| Status | enum | Submitted, UnderReview, InProgress, Completed, Declined |
| Priority | enum | Low, Medium, High, Urgent |
| SubmittedAt | DateTimeOffset | |
| ResolvedAt | DateTimeOffset? | |

---

### WorkOrder

The marina's internal work order, optionally linked to a customer maintenance request.

| Field | Type | Notes |
| --- | --- | --- |
| Id | UUID v7 | PK |
| TenantId | UUID v7 | Global query filter |
| MaintenanceRequestId | UUID v7? | FK → MaintenanceRequest (nullable — can be internal) |
| Title | string | |
| Description | string | |
| AssignedToUserId | UUID v7? | FK → User (staff member) |
| Status | enum | Open, InProgress, OnHold, Completed, Cancelled |
| Priority | enum | Low, Medium, High, Urgent |
| ScheduledDate | DateOnly? | |
| CompletedAt | DateTimeOffset? | |
| Notes | string? | |
| CreatedAt | DateTimeOffset | |

---

### Announcement

A news/update post from the marina to their customers.

| Field | Type | Notes |
| --- | --- | --- |
| Id | UUID v7 | PK |
| TenantId | UUID v7 | Global query filter |
| MarinaId | UUID v7 | FK → Marina |
| Title | string | |
| Body | string | Markdown or rich text |
| PublishedAt | DateTimeOffset? | Null = draft |
| ExpiresAt | DateTimeOffset? | Optional — hide after date |
| IsPinned | bool | Pinned announcements appear first |
| CreatedByUserId | UUID v7 | FK → User |
| CreatedAt | DateTimeOffset | |

---

### AuditLog

Append-only record of all mutations. No deletes, no updates.

| Field | Type | Notes |
| --- | --- | --- |
| Id | UUID v7 | PK |
| TenantId | UUID v7? | Null for platform operator actions |
| UserId | UUID v7 | Who performed the action |
| Action | string | e.g., "slip.assigned", "invoice.created", "payment.recorded" |
| EntityType | string | e.g., "Slip", "Invoice" |
| EntityId | UUID v7 | |
| Before | JSONB? | Previous state (null for creates) |
| After | JSONB? | New state (null for deletes) |
| IpAddress | string? | |
| Timestamp | DateTimeOffset | |

---

## Future Entities (Post-MVP)

These are anticipated but not designed in detail yet:

- **FleetBoat** — a boat owned/operated by the marina (tow boat, fuel tender, rental vessel); scoped to `MarinaId`, not a `CustomerAccount`
- **BoatRental** — links a `FleetBoat` to a `CustomerAccount` for a charter/rental period; includes dates, rate, deposit amount, and return condition
- **Waitlist** — customers queued for a slip type or dock
- **InventoryItem** — fuel, pump-out units, supplies
- **InventoryTransaction** — stock in/out
- **NotificationTemplate** — email/SMS templates
- **NotificationLog** — sent notification history
- **Document** — uploaded files (boat registration, insurance, contracts) linked to any entity
- **PaymentProvider** — configuration for integrated payment processors
- **SubscriptionPlan** — tier definitions for platform billing
