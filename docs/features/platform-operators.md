# Feature Area: Platform Operators

Platform operators are MyMarina staff who administer the SaaS itself. They have global visibility (with full audit logging) and run the trust-and-safety surface for the marketplace.

---

## Tenant Management

**Goal:** Provision, configure, and manage host accounts.

| Feature | Description | MVP |
| --- | --- | --- |
| List tenants | All tenants with status, tier, type (commercial / private host / demo), creation date, slip count, marina count | Yes |
| Tenant detail | Marinas, owners, billing info, audit log links | Yes |
| Create tenant | Provision a new tenant (rare — most self-onboard) | Yes |
| Edit tenant | Name, slug, contact info, tier, marketing opt-in | Yes |
| Suspend / reactivate tenant | Block access without deleting data | Yes |
| Set subscription tier | Upgrade/downgrade Free/Pro/Premium | Yes |
| Filter to private hosts | Separate view for private dock / dockominium tenants | Yes |
| Filter to demo tenants | Separate view; demo tenants are managed differently (see Demo Management below) | Yes |
| Soft-delete tenant | Soft-delete with retention period | No (post-MVP) |
| Per-tenant feature flags | Enable/disable specific features for individual tenants | No (post-MVP) |

---

## User Management & Moderation

**Goal:** Manage users across all tenants. Handle abuse on the marketplace.

| Feature | Description | MVP |
| --- | --- | --- |
| User search | Across all tenants by email, name, ID | Yes |
| User detail | Memberships, billing-account links, vessels, reservation history, login history | Yes |
| Reset user password | Trigger a password reset email | Yes |
| Revoke all refresh tokens | Force a user to re-authenticate | Yes |
| Disable user | `User.IsActive = false` — invalidates all tokens immediately | Yes |
| Re-enable user | Restore access | Yes |
| Confirm email manually | For users locked out of email | Yes |
| Impersonate user | Sign in as a user for support (audit-logged) | No (post-MVP) |
| Merge user accounts | Handle duplicate sign-ups | No (post-MVP) |

---

## Listing Moderation

**Goal:** Keep the marketplace healthy.

| Feature | Description | MVP |
| --- | --- | --- |
| Listing report queue | Reports filed by users; filterable by reason | Yes |
| Listing detail | All `AvailabilityWindow`s for a slip; pricing history; host info | Yes |
| Take down listing | Hide from search; notify host with reason | Yes |
| Reinstate listing | Restore after host responds | Yes |
| Photo review | Inappropriate photo flagging | Yes (manual) |
| Auto-flag suspicious listings | Heuristic-based moderation queue | No (post-MVP) |
| Listing audit | Who created / edited / paused; full history | Yes |

---

## Reservation & Dispute Support

**Goal:** Help when bookings go wrong.

| Feature | Description | MVP |
| --- | --- | --- |
| Reservation lookup | Find any reservation by ID, boater, host | Yes |
| View reservation full state | Status history, payment status, cancellation policy snapshot, revenue split snapshot | Yes |
| Cancel reservation (override) | Force-cancel with audit reason | Yes |
| Refund (Era 2) | Trigger a Stripe refund; reverse the split | No (Era 2) |
| Dispute case management | Capture a dispute, link parties, track resolution | No (post-MVP) |

---

## Demo Tenant Management

**Goal:** Keep the demo experience curated and current.

| Feature | Description | MVP |
| --- | --- | --- |
| Re-seed demo tenant | Reset the demo tenant to its seed state | Yes |
| View demo tenant data | Read-only view; verify the seed is rich | Yes |
| Edit demo seed script | Code-level; PR-driven | Yes (via repo) |
| Demo session metrics | How many auto-signin demo sessions per day | No (post-MVP) |

---

## Audit Log

**Goal:** Trace any action across the platform.

| Feature | Description | MVP |
| --- | --- | --- |
| Cross-tenant audit log viewer | All `AuditLog` entries | Yes |
| Filter by user, tenant, marina, action, date range | | Yes |
| Filter to platform-operator actions | Show only `platform.*` actions | Yes |
| Export | CSV download for compliance | No (post-MVP) |

---

## System Observability

**Goal:** Catch issues before users do.

| Feature | Description | MVP |
| --- | --- | --- |
| Health dashboard | API health, DB connectivity, Hangfire queue depth | No (basic via /ready endpoint) |
| Error log viewer | Recent application errors across tenants | No (use Sentry/external) |
| Tenant activity summary | Last-active date, invoice count, slip count, reservation count | Yes |
| Recurring job status | Hangfire dashboard at `/jobs` | Yes |

---

## Platform Billing (Future)

| Feature | Description | MVP |
| --- | --- | --- |
| Subscription billing | Charge tenants for tier via Stripe | No |
| Per-tenant invoice history | SaaS invoices per tenant | No |
| MRR / ARR dashboard | Revenue reporting | No |
| Per-reservation platform fee tracking | Era 2 (Stripe Connect splits) | No (Era 2) |
| Payout reconciliation | Match Stripe payouts to platform fees | No (Era 2) |

---

## Notes

- Platform operators authenticate with the same login system but hold the global `PlatformOperator` Identity role.
- Their JWT bypasses tenant filters via the `IsPlatformOperator` flag on `IUserContext`.
- All cross-tenant or platform-operator actions are logged in `AuditLog` with a `platform_action` flag in the action string.
- 2FA is mandatory for platform-operator accounts (post-MVP enforcement; recommended from launch).
- Impersonation, when implemented, will create a short-lived scoped token and log both the impersonator and the target user.
