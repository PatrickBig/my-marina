# Feature Area: Platform Operators

Platform operators are internal MyMarina staff who administer the SaaS product itself. They operate across all tenants.

---

## Tenant Management

**Goal:** Provision, configure, and manage marina accounts.

| Feature | Description | MVP |
|---|---|---|
| Create tenant | Provision a new marina account with name, slug, contact info, and subscription tier | Yes |
| View all tenants | List tenants with status, tier, creation date, slip count | Yes |
| Suspend / reactivate tenant | Prevent a marina from being accessed without deleting data | Yes |
| Delete tenant | Soft-delete with data retention period | No |
| Edit tenant details | Name, slug, contact info, tier | Yes |
| Set subscription tier | Upgrade/downgrade a marina's plan | Yes |
| Feature flags per tenant | Enable/disable specific features for individual tenants | No |

---

## User & Access Support

**Goal:** Help marina operators who are locked out or have access issues, with a full audit trail.

| Feature | Description | MVP |
|---|---|---|
| View tenant users | See all users for any marina | Yes |
| Reset user password | Trigger a password reset email for any user | Yes |
| Impersonate user | Log in as a marina operator for support purposes (audit-logged) | No |
| Revoke user session | Force a specific user's session to expire | Yes |
| View audit log | Cross-tenant audit log viewer | Yes |

---

## System Observability

**Goal:** Monitor platform health and catch issues early.

| Feature | Description | MVP |
|---|---|---|
| Health dashboard | API health, database connectivity, background job queue status | No |
| Error log viewer | Recent application errors across all tenants | No |
| Tenant activity summary | Last-active date, invoice count, slip count per tenant | No |

---

## Platform Billing (Future)

| Feature | Description | MVP |
|---|---|---|
| Subscription billing | Charge marinas for their tier via payment processor | No |
| Invoice history | Per-tenant billing history | No |
| MRR / ARR dashboard | Revenue reporting for the business | No |

---

## Notes

- Platform operators authenticate with the same login system but hold the `PlatformOperator` role
- Their JWT bypasses all tenant query filters
- All cross-tenant actions (reads and writes) are logged in `AuditLog` with `TenantId = null`
- Impersonation, when implemented, will create a short-lived scoped token and log both the impersonator and the target user
