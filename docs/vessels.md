# MyMarina — Vessels

> Cross-references: [overview.md](./overview.md) for the persona model, [data-model.md](./data-model.md) for field-level schema, [glossary.md](./glossary.md) for terminology.

## Why user-scoped boats?

In v0, boats were marina-scoped — `Boat.MarinaId` was required, and a boat at two marinas meant two records. That had four problems:

- **Owner data fragmentation.** A boater visiting multiple marinas had no canonical place for their vessel info.
- **No personal record.** A boater who wanted to track their own maintenance history had nowhere to put it.
- **No marketplace fit.** A marketplace requires a single source of truth for "what boat am I bringing?" — independent of which marina they're visiting.
- **Sale/transfer awkward.** Selling a boat between users at different marinas required manual reconciliation.

The new model splits the concern:

- **`Vessel`** — canonical, user-owned record. Make, model, dimensions, registration, photos. Travels with the user across all marinas.
- **`MarinaVesselRecord`** — marina-side overlay. Insurance verification, internal notes, work-order linkage. One per `(MarinaId, VesselId)` pair.

The boater owns the vessel data. The marina owns its annotations.

---

## Vessel — the canonical record

Owned by a `User` via `Vessel.OwnerUserId`. See [data-model.md#vessel](./data-model.md#vessel) for the full schema.

Owner-controlled fields:

- Name
- Make / Model / Year
- Length / Beam / Draft (used for slip-fit search)
- Boat type (Sailboat, Powerboat, Catamaran, Dinghy, PWC, Other)
- Hull color
- Registration number / state
- Photos (post-MVP)

The vessel record carries no marina-specific fields. Insurance, slip assignments, work orders all live on related entities, not on `Vessel` itself.

---

## MarinaVesselRecord — the marina's overlay

When a marina starts tracking a vessel — for billing, maintenance, insurance verification — they create a `MarinaVesselRecord` linking the canonical `Vessel` to their `Marina`.

Marina-controlled fields:

- `BillingAccountId` — which billing entity this vessel belongs to at this marina
- `InsuranceProvider`, `InsurancePolicyNumber`, `InsuranceExpiresOn`, `InsuranceVerifiedAt`, `InsuranceVerifiedByUserId`
- `Notes` — marina-private; never shown to the vessel owner

The marina's record never modifies the canonical Vessel. If a marina wants to record a different make/model/length than the owner declared, they file a discrepancy note in `Notes` — the canonical record remains the source of truth.

---

## Visibility rules

| Field | Visible to owner | Visible to marina staff |
| --- | --- | --- |
| `Vessel.*` (canonical) | ✓ (full edit) | ✓ (read-only) |
| `MarinaVesselRecord.Notes` | ✗ | ✓ |
| `MarinaVesselRecord.InsuranceVerifiedAt` | ✗ (MVP) | ✓ |
| `MarinaVesselRecord.InsuranceExpiresOn` | optional* | ✓ |
| Reservations referencing the vessel | ✓ (own only) | ✓ (at this marina) |
| Maintenance requests referencing the vessel | ✓ (own only) | ✓ (at this marina) |

*Owner visibility on insurance fields is post-MVP — eventually a "your insurance expires soon" notification.

---

## Ghost vessel claim flow

Marinas often have boats they want to record before the owner is on the platform. The "ghost vessel" pattern lets a marina record canonical data immediately and have it become a normal user-owned vessel later.

### Marina-side creation

When marina staff create a `BillingAccount` for a customer not on the platform:

1. Staff enter the customer's email.
2. Staff add the customer's vessel (make, model, length, etc.). A `Vessel` is created with `OwnerUserId = null` and `ClaimEmail` = the entered email.
3. The platform sends an invitation email to the customer with a claim link.
4. The marina's `MarinaVesselRecord` is created normally (insurance, notes, billing account), linked by `VesselId`.

The marina now has a complete record. They can issue invoices, track maintenance, run slip assignments — all referencing the ghost `Vessel`. Operationally, the marina is unblocked from day one.

### Boater-side claim

The boater clicks the claim link in the invitation email and lands on the claim page:

1. **If the email is unregistered:** They sign up. On first sign-in, the system finds all ghost vessels matching their email and presents them: "These boats were added by a marina. Confirm they're yours?" Each one can be accepted or rejected independently. Accepting sets `OwnerUserId` and records `ClaimedAt`.
2. **If the email is already registered:** They sign in normally. The same claim presentation appears.
3. **If the boater rejects a claim:** That `Vessel` remains unclaimed; the marina is notified. Marina can correct the email or remove the record.

### Edge cases

| Scenario | Behavior |
| --- | --- |
| Marina enters wrong email; never delivers | Vessel stays in ghost state. Marina can edit `ClaimEmail` to retry. |
| Same email used by two marinas with different vessels | Both ghost vessels appear at the boater's claim page. Each represents a distinct boat. |
| Boater claims a vessel that's actually a different boat (typo on marina's part) | Boater rejects; marina corrects. |
| Marina enters a typo and re-enters the correct email | Two ghost vessels with the same canonical data. Marina can merge or delete the typo one. |
| Vessel was already claimed at another marina by the same user | When the marina enters that user's email, the system matches and links a new `MarinaVesselRecord` to the existing `Vessel` directly. No new ghost is created. |
| Boater's email at marina ≠ boater's email at MyMarina | Boater can claim by signing in to MyMarina, going to "Pending claims," and entering the email the marina used. The system verifies via email round-trip. |

---

## Boat / Vessel naming convention

In code, JSON, database, and DTOs we use **`Vessel`**. In UI copy, marketing site, emails, and error messages we use **"Boat."** Same thing; we just don't talk like the Coast Guard at customers.

| Surface | Term used |
| --- | --- |
| Database tables | `vessels`, `marina_vessel_records` |
| C# types | `Vessel`, `VesselDto`, `CreateVesselCommand` |
| API paths | `/vessels/{id}`, `/marinas/{marinaId}/vessel-records` |
| API JSON fields | `vessel`, `vessels`, `vesselId` |
| UI labels | "My Boats", "Add a Boat", "Boat Details" |
| Email subjects | "Your boat at {marina}" |

The translation happens in the UI label layer only; backend, API contract, and codegen schema all stay consistent on `Vessel`.

---

## Multiple users on one vessel

A vessel has exactly one `OwnerUserId`. For shared boats (family-owned, partnership), the owner can grant access to others — but only via `BillingAccountMember` relationships at the marinas they visit, not by adding them as vessel co-owners.

Why? Because the boater identity is *one person bringing one boat*, not "this boat with whichever owner shows up." Reservations, insurance liability, and audit trails are clearer with a single human owner. If two people genuinely share a boat, they choose one to be the platform owner; the other gets added to relevant `BillingAccount`s.

This is a v1 simplification. A `VesselMember` junction is on the post-MVP backlog if demand appears.

---

## Future extensions

Listed in [data-model.md > Future Entities](./data-model.md#future-entities-post-mvp). Brief notes:

- **VesselTransfer** — sale or gift between users. Updates `OwnerUserId`; preserves all historical references (assignments, reservations, marina records). The new owner doesn't see the previous owner's reservation history; the marina sees a continuous history with an ownership-transfer marker on the timeline.
- **VesselMaintenanceLog** — owner-side service history. Independent of any marina's `MarinaVesselRecord`. The owner attaches receipts, photos, and notes for their own records.
- **VesselTrip** — owner-side trip log linking reservations and movements. Future "where has my boat been" timeline.
- **VesselDocument** — uploaded files (registration certificates, insurance docs). Owner-uploaded; visible to marinas the boat is currently associated with.
- **VesselReview** — covered in [marketplace.md](./marketplace.md); reviews live on Reservations, not on Vessels directly.

---

## Open questions

- **Vessel deletion vs archival.** Owner archives a vessel (`IsArchived = true`). Should the marina see it in their `MarinaVesselRecord` list? Recommendation: yes, with a clear "owner archived" badge, so historical billing/maintenance records still surface.
- **Boater request to see marina notes.** Marina records private notes about a vessel/owner. Owner has no view. Is there ever a case where the owner can demand visibility (e.g., GDPR-style data request)? Likely yes — handle as a manual support flow in MVP, automate post-MVP.
- **Auto-verified insurance.** Most boat insurers don't expose an API today. Revisit if a major carrier launches one.
- **Ghost vessel hygiene.** A ghost that's never claimed for 12 months — keep, archive, or prompt the marina to remove? Recommendation: surface a list to the marina annually; otherwise leave; deletion only by marina action.
