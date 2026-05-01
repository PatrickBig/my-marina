# MyMarina — Architecture

> Cross-references: [overview.md](./overview.md) for vision, [data-model.md](./data-model.md) for entity schemas, [auth-and-permissions.md](./auth-and-permissions.md) for auth detail, [marketplace.md](./marketplace.md) for booking flows.

## Solution Structure

```text
my-marina/
├── src/
│   ├── MyMarina.Domain/           # Entities, value objects, domain events, enums
│   ├── MyMarina.Application/      # Commands, queries, handler interfaces, DTOs, validators
│   ├── MyMarina.Infrastructure/   # EF Core, migrations, Identity, Hangfire, storage, email
│   ├── MyMarina.Api/              # Controller-based API endpoints, middleware, auth, OpenAPI config
│   ├── MyMarina.Web/              # React/Vite SPA (excluded from .sln)
│   └── MyMarina.Marketing/        # Marketing site (Astro/static)
├── tests/
│   ├── MyMarina.UnitTests/        # Domain logic, application handler unit tests
│   └── MyMarina.IntegrationTests/ # API + real Postgres via Testcontainers
├── k8s/                           # Kubernetes manifests
├── Dockerfile.api
├── Dockerfile.web
├── docker-compose.yml             # Local development
├── .github/workflows/             # CI/CD
└── MyMarina.sln
```

### Layer responsibilities

| Project | Depends On | Responsibility |
| --- | --- | --- |
| `Domain` | (nothing) | Entities, value objects, domain rules, enums |
| `Application` | `Domain` | Business logic, handler interfaces, DTOs, validators, `IUserContext`, `IMessageBus` |
| `Infrastructure` | `Application`, `Domain` | EF Core, ASP.NET Core Identity, Hangfire+Redis, external services, migrations |
| `Api` | `Application`, `Infrastructure` | HTTP endpoints, auth middleware, DI wiring, OpenAPI |
| `Web` | (API via HTTP) | React SPA, consumes OpenAPI-generated types |

---

## Authentication & Authorization

See [auth-and-permissions.md](./auth-and-permissions.md) for full detail. Summary at the architecture level:

- **ASP.NET Core Identity** for user storage, password hashing, lockout, email confirmation, 2FA scaffolding.
- **Custom JWT issuance** on top of Identity primitives — we deliberately do **not** use `MapIdentityApi`. Custom controllers in `MyMarina.Api/Controllers/AuthController.cs` use `UserManager<ApplicationUser>` and `SignInManager<ApplicationUser>` underneath.
- **Social login** (Google, Apple, Facebook) via standard Identity external-login flow.
- **JWT carries** `sub`, profile, `platform_role`, `memberships` (JSON array), `billing_accounts` (JSON array). No separate slip-ownership claim — slip permissions resolve through `Membership` at `Slip.MarinaId`.
- **Refresh tokens** are server-side, hashed, rotation-detected, revoked on permission changes.

---

## Authorization Model

There is **no single tenant filter**. Authorization is permission-derived row-level access. EF Core global query filters are still used — but the predicate consults the current user's claims (memberships, billing-account memberships, platform role) rather than a uniform `TenantId` match.

### `IUserContext`

A single per-request abstraction in `Application/Abstractions/IUserContext.cs` (planned, not yet built):

```csharp
public interface IUserContext
{
    Guid? UserId { get; }
    bool IsAuthenticated { get; }
    bool IsPlatformOperator { get; }

    IReadOnlyList<MembershipClaim> Memberships { get; }
    IReadOnlyList<BillingAccountMemberClaim> BillingAccounts { get; }

    bool HasMarinaAccess(Guid marinaId, MembershipRole minimumRole);
    bool HasTenantAccess(Guid tenantId, MembershipRole minimumRole);
    bool HasBillingAccountAccess(Guid billingAccountId, BillingAccountRole minimumRole);
}
```

Implemented by `HttpUserContext` in `Infrastructure/MultiTenancy/`, populated from JWT claims by the auth middleware. Replaces v0's `ITenantContext` / `IMarinaContext` / `ICustomerContext` — one interface, more capable, derived from claims rather than HTTP-thread state.

### Global query filter examples

| Entity | Default filter |
| --- | --- |
| `Marina` | listed + visible to public, OR user has Membership at `MarinaId` (or its Tenant), OR platform operator |
| `Slip` | hosted at a marina the user has access to, OR publicly listed (any open `AvailabilityWindow`), OR platform operator |
| `Invoice` | Marina-Membership at `Invoice.MarinaId`, OR BillingAccount-Membership at `Invoice.BillingAccountId`, OR platform operator |
| `Reservation` | `BoaterUserId = currentUser`, OR Marina-Membership at the slip's marina (or host marina), OR platform operator |
| `Vessel` | `OwnerUserId = currentUser`, OR referenced by a `MarinaVesselRecord` at a marina the user has Membership at, OR platform operator |
| `BillingAccount` | Membership at `MarinaId`, OR BillingAccountMember entry, OR platform operator |

The filter expression captures `IUserContext` at DbContext-build time and resolves the predicate per query. The `IN` lists are small and bounded by the user's accepted memberships.

### Authorization policies

Defined in `Api/Authorization/Policies.cs`. Resolved by custom `IAuthorizationHandler` implementations that read JWT claims and route values:

```text
PlatformOperator         — global IdentityRole
marina:owner             — Owner Membership at route's marinaId (or Tenant Owner)
marina:manager           — Owner or Manager
marina:staff             — Owner, Manager, or Staff
billing:owner            — Owner BillingAccountMember
billing:member           — any BillingAccountMember
reservation:participant  — BoaterUserId, OR marina:staff at the slip's marina (or host marina)
```

Tier gating uses `[RequiresTier(SubscriptionTier.X)]` on controller actions; the policy handler reads `tier` from the relevant Membership claim.

---

## CQRS Pattern (no MediatR)

MediatR is intentionally excluded due to its commercial license change. Explicit, typed handler interfaces:

```csharp
// Defined in Application/Abstractions/

public interface ICommandHandler<TCommand>
{
    Task HandleAsync(TCommand command, CancellationToken ct = default);
}

public interface ICommandHandler<TCommand, TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken ct = default);
}

public interface IQueryHandler<TQuery, TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken ct = default);
}
```

Cross-cutting concerns (logging, validation, authorization checks) are applied as **Scrutor decorators** registered in DI:

```csharp
services.AddScoped<ICommandHandler<CreateSlipCommand>, CreateSlipHandler>();
services.Decorate<ICommandHandler<CreateSlipCommand>, ValidationDecorator<CreateSlipCommand>>();
services.Decorate<ICommandHandler<CreateSlipCommand>, LoggingDecorator<CreateSlipCommand>>();
```

Decorators are generic and apply uniformly. All handler implementations are auto-registered by scanning `Application` and `Infrastructure` assemblies via Scrutor's `AddFromAssemblyOf<T>`.

---

## Modular Monolith

MyMarina is deployed as a single unit but structured internally as distinct modules with clear boundaries. This avoids distributed-systems complexity while preserving extraction seams if a module needs to split out later.

### Module structure

Modules are namespaces/folders within solution projects — not separate projects. Each module owns its commands, queries, events, and DTOs.

```text
MyMarina.Application/
├── Identity/             # Auth flows, registration, social login linking
├── Marinas/              # Marina + Dock + Slip CRUD; private-host onboarding
├── Memberships/          # Membership invitations, role changes
├── Vessels/              # Vessel CRUD, ghost-vessel claim flow
├── Billing/              # BillingAccount, Invoice, Payment
├── Marketplace/          # Reservation, AvailabilityWindow, search
├── Assignments/          # SlipAssignment + sublet flows
├── Maintenance/          # MaintenanceRequest, WorkOrder
├── Announcements/        # Marina announcements
├── Notifications/        # Subscribes to events from other modules; nothing depends on it
└── Platform/             # Platform-operator tooling, audit log viewer
```

### Module rules

- Modules communicate **only via events published through `IMessageBus`** — never by calling each other's handlers directly.
- The `Notifications` module is a pure subscriber; nothing depends on it.
- Cross-module queries (e.g., billing needs marina name) go through a shared read model or a dedicated query handler — not by importing another module's internals.
- If a module's load requires independent scaling, it can be extracted to its own process using the same `IMessageBus` abstraction with zero application code changes.

### When to extract a module

Extract on evidence, not anticipation:

| Signal | Candidate extraction |
| --- | --- |
| Notification queue delays affect booking-confirmation latency | `Notifications` → dedicated worker Deployment |
| Payment processing requires PCI-scoped isolation (Era 2) | `Billing` + Stripe-Connect handlers → isolated service |
| Reporting queries saturate the primary DB connection pool | Reporting → read replica or separate service |
| A dedicated team takes ownership of a module | Natural service boundary |

On Kubernetes, a worker is just a second Deployment running the same image with a different entrypoint — no separate repo or codebase required until team structure demands it.

---

## Background Jobs & Messaging

### Hangfire (MVP)

Hangfire with `Hangfire.Redis.StackExchange` provides the job queue:

- Redis-backed storage — no Postgres polling contention, far better throughput
- Batch job support included (no Hangfire Pro required)
- Recurring jobs (cron) for billing sweeps, overdue invoice detection, expired-reservation cleanup
- Hangfire dashboard exposed at `/jobs` behind `PlatformOperator` auth

### `IMessageBus` abstraction

All inter-module communication and background work goes through `IMessageBus`, defined in `Application` with no infrastructure dependency:

```csharp
public interface IMessageBus
{
    Task PublishAsync<T>(T message, CancellationToken ct = default) where T : class;
    Task ScheduleAsync<T>(T message, DateTimeOffset runAt, CancellationToken ct = default) where T : class;
}

public interface IMessageHandler<T> where T : class
{
    Task HandleAsync(T message, CancellationToken ct = default);
}
```

**MVP implementation:** `HangfireMessageBus` — enqueues messages as Hangfire background jobs dispatched to registered `IMessageHandler<T>` implementations.

**Future implementation:** `NatsMessageBus` — publishes to NATS JetStream subjects. Consumers run in a dedicated worker process or Kubernetes Deployment.

### NATS JetStream (future)

When the system needs true pub/sub, fan-out to multiple consumers, or event replay:

- Runs as a lightweight StatefulSet in the cluster
- JetStream provides durable, persistent streams with consumer groups and offset replay
- Swapping `HangfireMessageBus` for `NatsMessageBus` requires only a DI registration change

### Redis

Redis is a first-class infrastructure dependency:

- Hangfire job storage (via `Hangfire.Redis.StackExchange`)
- SignalR scale-out backplane — required for SignalR across multiple API pods
- General-purpose cache (output caching, distributed cache via `IDistributedCache`)

Cloud-agnostic: self-hosted in-cluster for dev, managed in production (AWS ElastiCache, Azure Cache for Redis, Upstash).

---

## API Design

- **Controller-based API** (`[ApiController]`) — more readable than Minimal APIs for a complex multi-resource surface; attribute-based response-code/header documentation feeds OpenAPI cleanly.
- **OpenAPI spec** auto-generated via `Microsoft.AspNetCore.OpenApi`.
- **Scalar** replaces Swagger UI for interactive API docs at `/scalar/v1`.
- **Single endpoint surface.** No `/portal/*` vs `/operator/*` split — endpoints are organized by resource, with authorization policies determining who can call what. A single user can act as host, customer, and boater simultaneously without routing tricks.
- All error responses use **RFC 9457 Problem Details** (`ValidationProblemDetails` for 422, `ProblemDetails` for all others).
- Response types documented explicitly with `[ProducesResponseType]` on every action.
- Pagination via cursor-based tokens (not offset) for scalability.
- Custom headers (e.g., `X-Request-Id`) documented via `[ResponseHeader]` attributes.

### Endpoint shape examples

```text
POST   /auth/register
POST   /auth/login
POST   /auth/refresh
GET    /auth/external/{provider}

GET    /me                                   — current user profile + memberships + billing accounts
PATCH  /me

GET    /vessels                              — boater's own vessels
POST   /vessels
GET    /vessels/{vesselId}
PATCH  /vessels/{vesselId}
POST   /vessels/{vesselId}/claim             — accept a ghost-vessel claim

GET    /marinas/{marinaId}                   — public marina profile (auth-aware: more fields if member/staff)
PATCH  /marinas/{marinaId}                   — [Authorize(marina:owner)]
POST   /marinas/{marinaId}/docks             — [Authorize(marina:manager)]
POST   /marinas/{marinaId}/slips
POST   /marinas/{marinaId}/staff/invite      — [Authorize(marina:owner)]
GET    /marinas/{marinaId}/billing-accounts  — [Authorize(marina:staff)]

GET    /slips/search                         — public marketplace search
GET    /slips/{slipId}                       — public listing detail

POST   /reservations                         — boater creates
GET    /reservations/{reservationId}         — [Authorize(reservation:participant)]
POST   /reservations/{reservationId}/cancel
POST   /reservations/{reservationId}/approve — owner / host marina

POST   /slip-assignments/{id}/away           — boater self-service "I'm away"
```

This is illustrative; the final shape lives in OpenAPI once the API project is rewritten.

---

## Audit Logging

Every mutation on business entities produces an `AuditLog` entry. Fields: `UserId?`, `MarinaId?`, `TenantId?`, `Action`, `EntityType`, `EntityId`, `Before` (JSONB), `After` (JSONB), `IpAddress`, `UserAgent`, `Timestamp`.

Audit logs are append-only — no deletes, no updates. Platform operators can query cross-tenant; everyone else is filtered to their own tenants/marinas.

Cross-tenant or platform-operator actions write `MarinaId = null` and `TenantId = null` (set if relevant) and carry an explicit `platform_action` flag in the action string (e.g., `platform.user_session_revoked`).

---

## Future: Stripe Connect Integration

Era 2 of the revenue model brings online payments. Architecture notes for forward-compatibility:

- **`PayoutAccount`** — one per Marina (commercial, yacht club, dockominium, private dock — every host has a Marina). Stores Stripe Connect account ID, status (Pending, Active, Restricted), capabilities.
- **Boater payment method** — Stripe Customer ID stored on `ApplicationUser` extension (`PaymentCustomerId`); payment methods (cards, bank accounts) live in Stripe.
- **Booking payment flow** — `Reservation.PaymentIntentId` populated at booking; capture occurs per the host's hold-vs-capture policy; transfers per `RevenueSplitSnapshot` after a configurable delay (default: 24h after `ArrivesAt`).
- **Refunds** — reverse the splits proportionally; stored as a `Payout` reversal record.
- **Module location** — `Application/Billing/` or a new `Application/Payments/` module subscribing to `ReservationConfirmed`, `ReservationCancelled`, `ReservationCompleted` events from `Marketplace`. No code in `Marketplace` directly references Stripe.
- **Compliance** — KYC/AML happens within Stripe Connect; we surface their flow but never store sensitive data.
- **Webhooks** — a `/webhooks/stripe` endpoint behind signature verification; events translate to `IMessageBus` publishes for handler isolation.

This wiring is **not** part of MVP. The data model carries the placeholders (`PaymentIntentId`, `PlatformFeeAmount`, `PaymentStatus`, `RevenueSplitSnapshot`) so adding Stripe is additive, not migratory.

---

## Deployment Architecture

```text
Internet
    │
    ▼
[nginx-ingress / Traefik]      ← TLS via cert-manager + Let's Encrypt
    │
    ├──▶ /api/*  →  [MyMarina.Api Pod(s)]   ← HPA for autoscaling
    │                     │
    │                     ├──▶ [PostgreSQL]  ← Managed in prod; in-cluster for dev
    │                     │
    │                     └──▶ [Redis]       ← Hangfire jobs, SignalR, cache
    │                                          Managed in prod; in-cluster for dev
    └──▶ /*      →  [MyMarina.Web Pod(s)]   ← Serves static React bundle
```

Future — when NATS JetStream is introduced:

```text
[MyMarina.Api Pod(s)]  ──publish──▶  [NATS JetStream]  ◀──subscribe──  [Worker Pod(s)]
                                             │
                                      durable streams,
                                      consumer groups,
                                      replay from offset
```

### Local development

`docker-compose.yml` spins up:

- Postgres
- Redis
- API (with hot reload via `dotnet watch`)
- Web (Vite dev server with HMR)

No Kubernetes required for day-to-day development.

---

## Demo Experience

The marketing site has a "Try the host dashboard" path that auto-signs visitors into a curated, read-only demo. Replaces v0's ephemeral-tenant model.

### Single static demo tenant

- One `Tenant` with `IsDemo = true`. Multiple `Marina` records under it spanning every type: `Commercial`, `YachtClub`, `PrivateCommunity`, `Dockominium`, `PrivateDock`.
- Seed-script-defined names — e.g., "Sunset Bay Marina" (commercial), "Eastside Yacht Club," "Maria's Slip at Sunset Bay" (dockominium under the same physical marina), "Pat's Dock" (private).
- Full inventory: docks, slips, customers (`BillingAccount` + `BillingAccountMember`), vessels (claimed and ghost), reservations across the full lifecycle, invoices in every status, maintenance requests, work orders, announcements.

### Read-only enforcement

- A central `WriteAccess` policy decorator wraps every non-GET endpoint.
- The decorator checks `IUserContext.IsDemo`; if true, returns 403 with a body explaining "this is a demo — sign up to make changes."
- Demo accounts have read-only `Membership` records; the policy is the catch-all enforcement point so handler code doesn't need to think about it.

### Search isolation

Demo listings are **filtered out of public search results.** A real boater searching for slips never sees demo data. The search filter in the marketplace module excludes `Tenant.IsDemo = true` unless `IUserContext.IsDemo` is true (i.e., the visitor is in a demo session).

### Demo session

- Marketing site → "Try the host dashboard" → backend issues a short-lived JWT scoped to a demo `User` with read-only `Membership` at the demo tenant.
- The token's payload carries `is_demo = true`; `IUserContext.IsDemo` reads from this claim.
- Session is anonymous (no email collected); expires after 30 minutes of inactivity.

### Seed script

- `DemoSeedScript.SeedAsync` in `MyMarina.Infrastructure` is the single source of truth.
- Idempotent: runs on every API startup. Wipes the demo tenant's mutable data and re-seeds; preserves demo `User` and `Membership` records (they're the auto-signin targets).
- A CI integration test asserts at least one record per known entity type lives in the demo tenant. A failing seed breaks the build.
- The marketing site's screenshots are captured against this seed via the Playwright skill, keeping marketing visuals truthful.

### Why static, not ephemeral

- **Cleanup overhead** of ephemeral tenants is real (zombie data accumulation, abuse vectors, scheduled-cleanup jobs).
- **Curation matters** — a demo with rich, realistic data tells the story; an empty new tenant doesn't.
- **Marketplace search** is naturally public for boaters, so the boater-side demo is just the live product. Only the host-side needs a demo session.

### Out of scope (post-MVP)

- Sandbox-mutation overlay (let visitors "try a booking" against a session-scoped overlay)
- Multiple demo personas (boater, marina-owner, private-host)
- Per-session demo tenant fork (heavier than the read-only model needs to be at MVP)

---

## Repository Strategy

Single git repository (monorepo), multiple Docker build artifacts.

- The `.sln` excludes `MyMarina.Web` and `MyMarina.Marketing` (both Node projects).
- GitHub Actions workflows are triggered by path filters: changes under `src/MyMarina.Api/**` or shared backend code trigger the API workflow; changes under `src/MyMarina.Web/**` trigger the Web workflow; same for Marketing.
- Images are pushed to GitHub Container Registry (`ghcr.io`).
- Kubernetes manifests live in `/k8s/` and are applied by the deploy step.

Splitting into separate repos is possible later if team structure demands it. The monorepo removes coordination overhead for full-stack changes during the MVP phase.
