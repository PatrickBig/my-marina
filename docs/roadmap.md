# MyMarina — Roadmap

## MVP Build Order

The MVP is built in layers, starting with the foundation and working outward to the customer-facing portal. Each phase should be shippable — a real marina could use what's built at the end of each phase.

---

### Phase 1 — Foundation ✅

*Everything else depends on this.*

- [x] Solution scaffold (projects, folder structure, `.sln`, `.gitignore`)
- [x] Update `CLAUDE.md` with build/test/lint commands and architecture notes
- [x] `MyMarina.Domain` — core entities, value objects, enums (UUID v7 PKs, CustomerAccount model, nullable DockId for moorings)
- [x] `MyMarina.Infrastructure` — EF Core, PostgreSQL, initial migration applied
- [x] Multi-tenancy plumbing (`ITenantContext`, `IMarinaContext`, `HttpTenantContext`, global query filters)
- [x] Health check endpoints (`/health`, `/ready`) + Scalar UI at `/scalar/v1`
- [x] `docker-compose.yml` for local dev (API + Postgres + Redis)
- [x] Vite + React + TypeScript + Tailwind v4 + shadcn/ui peer deps scaffold in `MyMarina.Web`
- [x] OpenAPI spec generation; `npm run generate-api` codegen wired into frontend

**Deliverable:** Running API (`:5222`) with multi-tenancy, real Postgres schema, Hangfire+Redis, and a blank frontend shell. Auth endpoints are first in Phase 2.

---

### Phase 2 — Marina Operator Core ✅

*A marina can configure their facility and manage customers.*

- [x] Auth — `POST /auth/login` with JWT (roles: PlatformOperator, MarinaOwner, MarinaStaff)
- [x] Tenant provisioning (platform operator creates a marina account)
- [x] Marina profile CRUD
- [x] Dock CRUD
- [x] Slip CRUD (with size constraints, rates, amenities, status)
- [x] Customer CRUD (create, view, edit, deactivate)
- [x] Boat registration CRUD
- [x] Slip assignment (create, end, conflict detection, rate override)
- [x] Slip availability check (filter slips by boat dimensions)
- [x] Staff invitation (`POST /staff/invite` — returns temporary password; email delivery is post-MVP)
- [x] EF global query filter bug fixed (was capturing `Guid.Empty` at model-build time)
- [x] Integration test suite — 30 tests covering all Phase 2 endpoints via Testcontainers

**Deliverable:** A marina operator can log in, set up their docks and slips, and add customers with their boats. ✅

---

### Phase 2 UI — Marina Operator Dashboard ✅

*React frontend for all Phase 2 API features. Marina operators get a real interface.*

#### Foundation

- [x] Run `npm run generate-api` and commit updated `schema.d.ts`
- [x] App shell: TanStack Router layout tree, protected route guard, 404 page
- [x] Auth state: Zustand store (`useAuthStore`) — token, user info, login/logout actions
- [x] Axios client wired to API base URL with Bearer token interceptor
- [x] Login page — form (React Hook Form + Zod), calls `POST /auth/login`, redirects on success

#### Marina Setup

- [x] Marina profile page — view and edit marina details, address, contact info
- [x] Dock list page — table with sort order, create/edit/delete dock
- [x] Slip list page — table with status badges, filters by dock; create/edit/delete slip
- [x] Slip availability checker — date range + boat dimension inputs, results table

#### Customers & Boats

- [x] Customer list page — searchable table, active/inactive toggle
- [x] Customer detail page — info, boats, current slip assignment, action buttons
- [x] Create / edit customer form (billing info, emergency contact)
- [x] Boat list per customer — create/edit/delete boat form

#### Slip Assignments

- [x] Slip assignment list — filterable by slip, customer, active-only
- [x] Assign slip form — slip picker (respects availability), date range, rate override
- [x] End assignment action with confirmation dialog

#### Staff

- [x] Staff invite form — email, name, role picker; displays temporary password on success

**Deliverable:** A marina operator can do their full daily workflow through the browser without touching the API directly. ✅

---

### Phase 3 — Billing (Manual) ✅

*The marina can invoice customers and record payments.*

#### Billing API

- [x] Invoice CRUD (create, edit draft, send, void)
- [x] Invoice line items (add/remove/edit)
- [x] Sequential invoice number generation per tenant
- [x] Payment recording (manual: cash, check, etc.)
- [x] Partial payment support
- [x] Overdue invoice flagging
- [x] Invoice status transitions with audit trail
- [x] Customer billing history view (operator side)

#### Billing UI

- [x] Invoice list page — status badges (Draft, Sent, Paid, Overdue, Void), filters
- [x] Invoice detail page — line items, payment history, status action buttons (Send, Void)
- [x] Create/edit invoice form — customer picker, line item editor, due date
- [x] Record payment dialog — amount, method, date, notes
- [x] Customer billing history tab on customer detail page

**Deliverable:** A marina can issue invoices and track who has paid. ✅

---

### Phase 4 — Customer Portal

*Customers can log in and see their information.*

#### Portal API

- [ ] Customer self-registration (via invitation link)
- [ ] View current slip assignment
- [ ] View registered boats
- [ ] View invoices and payment history
- [ ] Submit maintenance request
- [ ] View maintenance request status and history
- [ ] View announcement feed

#### Portal UI

- [ ] Customer login + registration pages (separate route from operator login)
- [ ] Customer dashboard — current slip, balance due at a glance
- [ ] My boats page
- [ ] My invoices page — list + detail view
- [ ] Submit maintenance request form
- [ ] My requests page — status history
- [ ] Announcements feed

**Deliverable:** Customers have a working self-service portal.

---

### Phase 5 — Announcements & Maintenance Workflow

*Full communication loop between marina and customers.*

#### Announcements & Maintenance API

- [ ] Announcement CRUD (marina operator side)
- [ ] Draft, publish, pin, expire announcements
- [ ] Maintenance request review and status updates (marina operator side)
- [ ] Work order creation and assignment (internal or from a request)
- [ ] Work order status tracking

#### Announcements & Maintenance UI

- [ ] Announcements manager — list, create/edit, publish/pin/expire actions
- [ ] Maintenance request inbox — list, status filter, detail view, status update
- [ ] Work order list — create from request or scratch, assignee, status board

**Deliverable:** Marina operators can communicate with customers and track maintenance work.

---

### Phase 6 — Platform Operator Tools

*The support and admin tooling for managing the SaaS itself.*

#### Platform API

- [ ] Platform operator dashboard (tenant list)
- [ ] Tenant creation, suspension, reactivation
- [ ] Cross-tenant user management (view, reset password, revoke session)
- [ ] Audit log viewer (cross-tenant)

#### Platform UI

- [ ] Platform admin shell (separate layout from marina operator UI)
- [ ] Tenant list — status badges, create tenant form, suspend/reactivate actions
- [ ] Tenant detail — marina list, owner info, subscription tier editor
- [ ] Cross-tenant user search + detail + reset password
- [ ] Audit log viewer — filterable by tenant, user, action, date range

**Deliverable:** A platform operator can administer the SaaS without touching the database.

---

## Post-MVP Backlog

These are planned but not sequenced. Order will be determined by customer feedback.

| Feature | Area |
|---|---|
| Invoice PDF generation | Billing |
| Email invoice delivery | Billing |
| Recurring invoice generation | Billing |
| Late fee automation | Billing |
| Payment provider integration (Stripe) | Billing |
| Tax rate configuration | Billing |
| Waitlist management | Slips |
| Slip map / visual occupancy view | Slips |
| Boat document uploads | Boats |
| Insurance expiry alerts | Boats |
| Reporting dashboard (occupancy, revenue, AR aging) | Analytics |
| Email notification engine | Notifications |
| SMS notifications (Twilio) | Notifications |
| Announcement targeting (by dock/slip/segment) | Announcements |
| Granular staff permissions | Access Control |
| Subdomain-per-tenant routing | Infrastructure |
| Supplies & inventory tracking | Operations |
| Mobile application | Mobile |
| On-premise / self-hosted packaging | Distribution |
| Feature flag system (tier gating) | Platform |
| Platform billing (charge marinas for SaaS tiers) | Platform |
| User impersonation for support | Platform |
| Merge duplicate customer accounts | CRM |

---

## Notes

- Each phase ends with a shippable state — no "almost working" phases
- Kubernetes manifests and CI/CD are set up and deployed — push to `main` triggers build → GHCR push → `kubectl rollout`
- OpenAPI spec is the contract — any breaking API change requires a version bump discussion
- The payment provider integration is designed as a pluggable abstraction in Phase 3 even though no provider is connected — the `Payment` entity has `PaymentProviderId` and `PaymentProviderReference` fields reserved from day one

## Dev Seed Credentials

Seeded automatically in `Development` environment on first startup:

| Account | Email | Password |
| --- | --- | --- |
| Platform operator | `admin@mymarina.org` | `Admin@Marina123!` |
| Demo marina owner | `owner@demo-marina.com` | `Owner@Marina123!` |
