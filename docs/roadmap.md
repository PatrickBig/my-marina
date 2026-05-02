# MyMarina — Roadmap

## Status

**Marketplace pivot.** The v0 phases (Phases 1–6, all completed) are being scrapped. The codebase, schema, and migrations will be hard-reset before this roadmap begins. v0's deliverables exist in git history but are not the foundation for what's next; the data model, auth model, and product framing have all changed materially. See [overview.md](./overview.md), [data-model.md](./data-model.md), and [auth-and-permissions.md](./auth-and-permissions.md) for the new model.

This roadmap covers the build of the marketplace MVP — the two-sided platform connecting boaters with marina and private slip owners.

---

## MVP Build Order

The MVP is built in layers. Each phase ends with a shippable state — a real user could meaningfully use what's built so far.

---

### Phase 0 — Hard Reset ✅

*Clear v0; preserve the project shell.*

- [x] Drop all EF Core migrations
- [x] Wipe `MyMarina.Domain`, `MyMarina.Application`, `MyMarina.Infrastructure` (entities, handlers, DI registrations)
- [x] Wipe `MyMarina.Api/Controllers/*` (auth, marina, customer, billing, etc.)
- [x] Wipe `MyMarina.Web/src/routes/*` and stateful stores
- [x] Drop the development Postgres volume
- [x] Keep: project structure, `.sln`, Dockerfiles, docker-compose, K8s manifests, marketing site, CI workflows, OpenAPI scaffolding, integration test harness, the demo seed script structure (rewritten in Phase 15)

**Deliverable:** Buildable but mostly-empty solution. API runs and returns 401 on every endpoint. Frontend renders an empty shell.

---

### Phase 1 — Identity Foundation ✅

*Sign up. Sign in. Get a JWT.*

- [x] ASP.NET Core Identity with global `ApplicationUser` (no `TenantId`)
- [x] `POST /auth/register` — email+password registration with email confirmation
- [x] `POST /auth/login` — credential validation; issues access + refresh JWT
- [x] `POST /auth/refresh` — refresh token rotation; reuse-detection revokes all sibling tokens
- [x] `POST /auth/forgot-password` / `POST /auth/reset-password`
- [x] `POST /auth/confirm-email` / `POST /auth/resend-confirmation`
- [x] `RefreshToken` table with hashed token storage
- [x] `IUserContext` abstraction populated from JWT
- [x] `GET /me` — current user profile + memberships + billing accounts (empty for new users)
- [x] Login page in `MyMarina.Web` — email/password form with Zod validation

**Deliverable:** A user can register, confirm their email, log in, and see their profile.

---

### Phase 2 — Social Login ✅

*Google, Apple, Facebook.*

- [x] `GET /auth/external/{provider}` — OAuth challenge
- [x] `GET /auth/external/{provider}/callback` — sign in or register
- [x] `GET /auth/external/{provider}/link` / `link-callback` — link provider to signed-in account
- [x] `POST /auth/external/{provider}/unlink` — remove linked provider
- [x] `GET /auth/external/providers` — list linked providers
- [x] Provider configuration in K8s secrets (`Auth:Google`, `Auth:Facebook`, `Auth:Apple` in appsettings)
- [x] Account-linking rules (existing email collision requires existing-method sign-in first)
- [x] Social login buttons on login page; `AuthCallbackPage.tsx` handles OAuth redirect

**Deliverable:** A user can sign in via Google, Apple, or Facebook. Existing accounts can link multiple providers.

---

### Phase 3 — User Profile & Vessels ✅

*Boaters can manage their boats.*

- [x] `Vessel` entity in Domain (OwnerUserId, dimensions, BoatType, ghost-vessel fields)
- [x] `BoatType` enum: `Sailboat`, `Powerboat`, `Catamaran`, `Dinghy`, `Pwc`, `Other`
- [x] `POST /vessels` — create a vessel for the authenticated user
- [x] `GET /vessels` — list my vessels (non-archived)
- [x] `GET /vessels/{id}` — get single vessel
- [x] `PATCH /vessels/{id}` — edit vessel fields
- [x] `DELETE /vessels/{id}` — soft archive
- [x] `POST /vessels/{id}/claim` — stub (no-op until Phase 5)
- [x] EF Core migration `Phase3_Vessels`
- [x] Profile editing UI — `ProfilePage.tsx` (`PATCH /me` backend already existed)
- [x] "My Boats" page — `MyBoatsPage.tsx` with create/edit/archive
- [x] `NavBar.tsx` — navigation between profile and boats pages

**Deliverable:** A boater can sign up, add boats to their profile, and edit their information.

---

### Phase 4 — Marina Onboarding (Commercial) ✅

*Commercial marinas come on the platform.*

- [x] Tenant + Marina creation flow (host signup)
- [x] `Marina` profile CRUD (name, address, phone, timezone, lat/long, type=`Commercial` for now)
- [x] `Dock` CRUD
- [x] `Slip` CRUD (with all dimension/amenity/status fields)
- [x] `Membership` invitations: `POST /marinas/{id}/staff/invite`
- [x] Staff sign-in with marina membership claims in JWT
- [x] Marina dashboard skeleton (host view)

**Deliverable:** A commercial marina can sign up, configure docks and slips, and invite staff.

---

### Phase 5 — Customers & Ghost Vessels

*Marinas track customers (with or without platform accounts).*

- [ ] `BillingAccount` CRUD
- [ ] `BillingAccountMember` (junction)
- [ ] Ghost vessel creation: marina staff can add a `Vessel` for a non-platform customer
- [ ] Email-based vessel claim flow (invitation email with claim link)
- [ ] Acceptance updates `Vessel.OwnerUserId` and creates `BillingAccountMember`
- [ ] `MarinaVesselRecord` (insurance, notes, billing-account link)

**Deliverable:** A marina can record customers, their vessels, and insurance — even before the customer signs up. Once the customer accepts, ownership transfers cleanly.

---

### Phase 6 — Long-Term Assignments

*Marinas assign slips to customers.*

- [ ] `SlipAssignment` CRUD (with sublet policy flags from day one)
- [ ] Slip availability check (date range + vessel dimensions)
- [ ] Conflict detection (prevent double-booking)
- [ ] Web UI: assignment list, create/edit, end assignment

**Deliverable:** A marina can assign a customer and their boat to a slip for a date range.

---

### Phase 7 — Marketplace Listings

*Hosts list slips for transient bookings.*

- [ ] `AvailabilityWindow` CRUD
- [ ] Window non-overlap enforcement
- [ ] `InstantBook` toggle
- [ ] Pricing fields (base price, weekly/monthly discount, cleaning fee, min/max nights)
- [ ] `RevenueSplit` (defaulted to 100% to slip owner; overrides come in Phase 10)
- [ ] Calendar UI for hosts: drag a date range, set price/policy

**Deliverable:** A host can list a slip on the marketplace with pricing and policy.

---

### Phase 8 — Discovery & Search

*Boaters find slips.*

- [ ] `GET /slips/search` — public, unauthenticated
- [ ] Bounding-box geo filter
- [ ] Vessel-fit filter (length, beam, draft)
- [ ] Date-range availability filter
- [ ] Public slip detail page
- [ ] Search-results UI with map view (lightweight; e.g., Leaflet)
- [ ] Filter out demo listings unless the visitor is in a demo session

**Deliverable:** A visitor (logged in or not) can search slips by location, dates, and boat dimensions, and see real listings.

---

### Phase 9 — Reservations

*Boaters book slips.*

- [ ] `Reservation` entity
- [ ] Status state machine (`PendingHostMarinaApproval` → `PendingApproval` → `Confirmed` → `Completed`/`Cancelled`/`NoShow`)
- [ ] Request-to-book flow (owner approves or declines)
- [ ] Instant-book flow (auto-confirms)
- [ ] Boater portal: "My Trips" — upcoming, past, cancelled
- [ ] Host inbox: incoming reservations with approve/decline actions
- [ ] Email notifications on status transitions
- [ ] Cancellation by boater or host (records snapshot of policy; no money moves in MVP)

**Deliverable:** A boater can reserve a slip. The host can approve, decline, or instant-book. The reservation is tracked through its lifecycle.

---

### Phase 10 — Sublet Flows

*Marinas and boaters can sublet leased slips.*

- [ ] "I'm away" UI + endpoint (`POST /slip-assignments/{id}/away`)
- [ ] Holder-initiated sublet listing (`AvailabilityWindow` with `ListedByKind = Holder`)
- [ ] Owner-initiated sublet listing (`ListedByKind = OwnerForHolder`) tied to a holder's "away" entry
- [ ] `RevenueSplit` snapshot on sublet windows (using lease policy fields)
- [ ] Lease policy enforcement in availability-window creation

**Deliverable:** A long-term tenant can mark themselves away and either list it themselves or have the marina list it on their behalf, with revenue split per the lease.

---

### Phase 11 — Invoicing & Payments (Manual)

*Marinas bill, customers pay (off-platform).*

- [ ] `Invoice` CRUD (carry over from v0; updated to use `BillingAccountId` and optional `ReservationId`)
- [ ] `InvoiceLineItem` and `Payment` (manual recording: cash, check, card, etc.)
- [ ] Sequential invoice number per marina
- [ ] Overdue auto-flagging (Hangfire recurring job)
- [ ] Customer portal: "My Invoices" — view, history, balance
- [ ] Marina invoice list, detail, void, partial payment

**Deliverable:** A marina can issue invoices linked to assignments or reservations. A customer can see invoices and payment history. Money still moves off-platform.

---

### Phase 12 — Maintenance & Announcements

*Marina–customer communication.*

- [ ] `MaintenanceRequest` CRUD (boater submits, marina triages)
- [ ] `WorkOrder` CRUD (marina internal; optionally linked to a request)
- [ ] `Announcement` CRUD (marina publishes, customers + incoming boaters see)
- [ ] Boater portal: submit/view requests; view announcements feed

**Deliverable:** Boaters report problems; marinas track and resolve. Marinas post news; boaters see it.

---

### Phase 13 — Private Slip Owners

*Dockominium and private dock onboarding.*

- [ ] "Add my dock" wizard (`MarinaType = PrivateDock`)
- [ ] "Add a slip I own at a marina" wizard (`MarinaType = Dockominium`, requires `Slip.HostMarinaId`)
- [ ] Auto-creates Tenant + Marina + Slip + Owner Membership
- [ ] `HostMarinaPolicy` (None / NotifyOnly / RequiresApproval)
- [ ] Host marina notification flow when a dockominium slip is added
- [ ] Host marina approval flow in reservation lifecycle (when policy = `RequiresApproval`)
- [ ] UX: brand the experience as "your dock" / "your slip," never "your marina"

**Deliverable:** Individuals can list a private dock or a dockominium slip on the marketplace. Host marina policies are enforced.

---

### Phase 14 — Platform Operator Tools

*MyMarina staff manage the platform.*

- [ ] `PlatformOperator` Identity role
- [ ] Tenant list + create/suspend/reactivate
- [ ] User search across tenants
- [ ] Force sign-out (revoke all refresh tokens)
- [ ] Cross-tenant audit log viewer
- [ ] Listing moderation queue
- [ ] User moderation actions

**Deliverable:** MyMarina staff can manage tenants, users, and listings without touching the database.

---

### Phase 15 — Demo Experience

*Replace ephemeral demo tenants with a curated read-only demo.*

- [ ] `Tenant.IsDemo` flag (single demo tenant, multiple demo marinas)
- [ ] `WriteAccess` policy decorator on every non-GET endpoint — returns 403 with a "this is a demo" body when the caller's context is demo
- [ ] Demo seed script (idempotent, runs on each deploy)
- [ ] Demo seed includes: 1 commercial marina, 1 yacht club, 1 dockominium, 1 private dock; full inventory (docks, slips, customers, vessels, reservations, invoices, maintenance, announcements, work orders)
- [ ] "Try the host dashboard" auto-signin button on marketing site (creates a short-lived demo session token tied to a read-only `Membership` at a demo marina)
- [ ] Demo listings are excluded from real-user search results (filtered via `Tenant.IsDemo` in the search query unless the caller is in a demo session)
- [ ] CI integration test asserts at least one record per known entity type lives in the demo tenant
- [ ] Marketing site's screenshots are captured against the demo via the Playwright skill

**Deliverable:** A visitor on the marketing site can click into a working host dashboard backed by a curated demo, with no risk of polluting data or being abused. Real users never see demo listings in search.

---

### Phase 16 — Pre-Launch Hardening

*Get ready for a real public.*

- [ ] End-to-end Playwright tests covering critical flows
- [ ] Performance baseline (search, reservation creation, login)
- [ ] Security review (auth flows, input validation, PII handling)
- [ ] Marketing site polish (screenshots from the live demo, onboarding copy)
- [ ] Production deploy + smoke tests
- [ ] Monitoring/alerting (Sentry or similar; basic API metrics; uptime checks)
- [ ] Documentation polish (CLAUDE.md, README, support docs)

**Deliverable:** Public launch.

---

## Post-MVP Backlog

Sequenced by user feedback after launch.

| Feature | Area |
| --- | --- |
| Stripe Connect — online reservation payments + payouts | Payments |
| Reviews & ratings (boater ↔ host) | Trust |
| In-app messaging (boater ↔ host pre-booking) | Communication |
| Insurance verification automation | Vessels |
| Host calendar iCal export | Listings |
| Vessel ownership transfer (boat sale flow) | Vessels |
| Vessel maintenance log (owner-side) | Vessels |
| Vessel trip log | Vessels |
| Boat document uploads (registration, insurance docs) | Vessels |
| Mobile application | Mobile |
| PostGIS-based search (along navigable water) | Search |
| Recurring invoice generation | Billing |
| Late fee automation | Billing |
| Invoice PDF generation | Billing |
| Email invoice delivery | Billing |
| Tax rate configuration | Billing |
| Reporting dashboard (occupancy, revenue, AR aging) | Analytics |
| Email notification engine (templates, opt-out, audit) | Notifications |
| SMS notifications (Twilio) | Notifications |
| Announcement targeting (by dock, slip, customer segment) | Announcements |
| Granular staff permissions (billing-only, maintenance-only) | Access Control |
| Slip map / visual occupancy view | Slips |
| Waitlist management | Slips |
| Subdomain-per-tenant routing (white-label) | Infrastructure |
| Supplies & inventory tracking | Operations |
| Marina-owned charter fleet (FleetVessel + VesselRental) | Operations |
| On-premise / self-hosted packaging | Distribution |
| Platform billing (charge marinas for SaaS tiers) | Platform |
| User impersonation for support | Platform |
| 2FA for marina owners and platform operators | Security |
| Sandbox-mutation overlay in demo (try-a-booking) | Demo |
| Multiple demo personas (boater, owner, private-host) | Demo |
| Currency / multi-currency support | Internationalization |
| Group reservations (multi-slip bookings) | Reservations |
| Pricing rules engine (dynamic, day-of-week, holidays) | Pricing |

---

## Notes

- Each phase ends with a shippable state — no "almost working" phases.
- Kubernetes manifests and CI/CD continue from v0 — push to `main` triggers build → GHCR push → `kubectl rollout`.
- OpenAPI spec is the contract — any breaking API change requires a version bump discussion.
- Stripe Connect is **not** part of MVP, but the data model carries placeholders (`PaymentIntentId`, `PlatformFeeAmount`, `RevenueSplitSnapshot`) so it lands additively in Era 2.
- The demo seed script is a living artifact — see [architecture.md](./architecture.md#demo-experience). A failing seed breaks CI.

---

## Dev Seed Credentials

Seeded automatically in `Development` environment on first startup:

| Account | Email | Password |
| --- | --- | --- |
| Platform operator | `admin@mymarina.org` | `Admin@Marina123!` |
| Demo marina owner | `owner@demo-marina.com` | `Owner@Marina123!` |

These match the v0 credentials so existing bookmarks and dev setups still work.
