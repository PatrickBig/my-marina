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

### Phase 2 — Marina Operator Core
*A marina can configure their facility and manage customers.*

- [ ] Tenant provisioning (platform operator creates a marina account)
- [ ] Marina profile CRUD
- [ ] Dock CRUD
- [ ] Slip CRUD (with size constraints, rates, amenities, status)
- [ ] Customer CRUD (create, view, edit, deactivate)
- [ ] Boat registration CRUD
- [ ] Slip assignment (create, end, conflict detection, rate override)
- [ ] Slip availability check (filter slips by boat dimensions)
- [ ] Staff invitation and management

**Deliverable:** A marina operator can log in, set up their docks and slips, and add customers with their boats.

---

### Phase 3 — Billing (Manual)
*The marina can invoice customers and record payments.*

- [ ] Invoice CRUD (create, edit draft, send, void)
- [ ] Invoice line items (add/remove/edit)
- [ ] Sequential invoice number generation per tenant
- [ ] Payment recording (manual: cash, check, etc.)
- [ ] Partial payment support
- [ ] Overdue invoice flagging
- [ ] Invoice status transitions with audit trail
- [ ] Customer billing history view (operator side)

**Deliverable:** A marina can issue invoices and track who has paid.

---

### Phase 4 — Customer Portal
*Customers can log in and see their information.*

- [ ] Customer self-registration (via invitation link)
- [ ] View current slip assignment
- [ ] View registered boats
- [ ] View invoices and payment history
- [ ] Submit maintenance request
- [ ] View maintenance request status and history
- [ ] View announcement feed

**Deliverable:** Customers have a working self-service portal.

---

### Phase 5 — Announcements & Maintenance Workflow
*Full communication loop between marina and customers.*

- [ ] Announcement CRUD (marina operator side)
- [ ] Draft, publish, pin, expire announcements
- [ ] Maintenance request review and status updates (marina operator side)
- [ ] Work order creation and assignment (internal or from a request)
- [ ] Work order status tracking

**Deliverable:** Marina operators can communicate with customers and track maintenance work.

---

### Phase 6 — Platform Operator Tools
*The support and admin tooling for managing the SaaS itself.*

- [ ] Platform operator dashboard (tenant list)
- [ ] Tenant creation, suspension, reactivation
- [ ] Cross-tenant user management (view, reset password, revoke session)
- [ ] Audit log viewer (cross-tenant)

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
- Kubernetes manifests and CI/CD are set up during Phase 1 so every phase is deployed the same way
- OpenAPI spec is the contract — any breaking API change requires a version bump discussion
- The payment provider integration is designed as a pluggable abstraction in Phase 3 even though no provider is connected — the `Payment` entity has `PaymentProviderId` and `PaymentProviderReference` fields reserved from day one
