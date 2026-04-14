# MyMarina — Architecture

## Solution Structure

```
my-marina/
├── src/
│   ├── MyMarina.Domain/           # Entities, value objects, domain events, enums
│   ├── MyMarina.Application/      # Commands, queries, handler interfaces, DTOs, validators
│   ├── MyMarina.Infrastructure/   # EF Core, migrations, storage, email, background jobs
│   ├── MyMarina.Api/              # Controller-based API endpoints, middleware, auth, OpenAPI config
│   └── MyMarina.Web/              # React/Vite frontend (not a .NET project; excluded from .sln)
├── tests/
│   ├── MyMarina.UnitTests/        # Domain logic, application handler unit tests
│   └── MyMarina.IntegrationTests/ # API + real Postgres via Testcontainers
├── k8s/                           # Kubernetes manifests
│   ├── api-deployment.yaml
│   ├── web-deployment.yaml
│   ├── postgres-deployment.yaml   # Dev/staging only; use managed Postgres in prod
│   └── ingress.yaml
├── Dockerfile.api
├── Dockerfile.web
├── docker-compose.yml             # Local development
├── .github/
│   └── workflows/
│       ├── api.yml
│       └── web.yml
└── MyMarina.sln
```

### Layer Responsibilities

| Project | Depends On | Responsibility |
|---|---|---|
| `Domain` | Nothing | Entities, value objects, domain rules, enums |
| `Application` | `Domain` | Business logic, handler interfaces, DTOs, validators |
| `Infrastructure` | `Application`, `Domain` | EF Core, external services, migrations |
| `Api` | `Application`, `Infrastructure` | HTTP endpoints, auth middleware, DI wiring |
| `Web` | (API via HTTP) | React SPA, consumes OpenAPI-generated types |

---

## Multi-Tenancy

**Strategy: Shared database, shared schema, row-level isolation.**

### How it works

1. Every tenant-scoped entity carries a `TenantId` (GUID).
2. EF Core **global query filters** are applied to all tenant-scoped `DbSet`s, automatically appending `WHERE TenantId = @currentTenantId` to every query. No query is manually filtered.
3. The current tenant is resolved via `ITenantContext`, which is populated by the authentication middleware from the user's JWT claim.
4. Platform operators carry a special claim that bypasses tenant filters, allowing cross-tenant reads for support purposes (with full audit logging).

### Tenant resolution abstraction

```csharp
public interface ITenantResolver
{
    Task<Guid?> ResolveAsync(HttpContext context);
}

// MVP implementation: resolve from JWT claim
public class JwtTenantResolver : ITenantResolver { ... }

// Future: resolve from Host header (subdomain routing)
public class SubdomainTenantResolver : ITenantResolver { ... }
```

Switching to subdomain routing requires only registering a different `ITenantResolver` implementation — no application logic changes.

### Base entity

All tenant-scoped entities inherit from:

```csharp
public abstract class TenantEntity
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
```

---

## CQRS Pattern (No MediatR)

MediatR is intentionally excluded (commercial license change). We use explicit, typed handler interfaces.

### Interfaces (defined in `Application`)

```csharp
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

### Cross-cutting concerns via Scrutor decorators

Logging, validation, and authorization checks are applied as decorators registered in DI:

```csharp
services.AddScoped<ICommandHandler<CreateSlipCommand>, CreateSlipHandler>();
services.Decorate<ICommandHandler<CreateSlipCommand>, ValidationDecorator<CreateSlipCommand>>();
services.Decorate<ICommandHandler<CreateSlipCommand>, LoggingDecorator<CreateSlipCommand>>();
```

Decorators are generic and apply uniformly — no per-handler boilerplate.

### Auto-registration

All handler implementations are discovered and registered by scanning the `Application` and `Infrastructure` assemblies using Scrutor's `AddFromAssemblyOf<T>`.

---

## Authentication & Authorization

- **ASP.NET Core Identity** for user storage and password hashing
- **JWT Bearer tokens** for stateless API authentication
- **Refresh token rotation** stored in the database
- **Roles**: `PlatformOperator`, `MarinaOwner`, `MarinaStaff`, `Customer`
- **Policies**: Fine-grained permission policies per feature area (e.g., `billing:write`, `slip:manage`)
- Future: OIDC provider integration (Auth0, Azure AD, etc.) via the existing OpenIdConnect middleware

---

## Audit Logging

Every mutation (create, update, delete) on business entities produces an `AuditLog` entry:

```
AuditLog
  Id           GUID
  TenantId     GUID (nullable — platform ops actions have no tenant)
  UserId       GUID
  Action       string  (e.g. "slip.assigned", "invoice.created")
  EntityType   string
  EntityId     GUID
  Before       JSONB   (previous state, nullable)
  After        JSONB   (new state)
  Timestamp    DateTimeOffset
  IpAddress    string
```

Audit logs are append-only. No deletes, no updates.

---

## Modular Monolith

MyMarina is deployed as a single unit but structured internally as distinct modules with clear boundaries. This avoids the distributed systems complexity of microservices while preserving clean extraction seams if specific modules need to be split out later.

### Module structure

Modules are namespaces/folders within the solution projects — not separate projects or assemblies. Each module owns its commands, queries, events, and DTOs.

```text
MyMarina.Application/
├── Billing/
│   ├── Commands/        # CreateInvoice, RecordPayment, VoidInvoice, ...
│   ├── Queries/         # GetInvoice, ListInvoices, GetAgingReport, ...
│   └── Events/          # InvoiceCreated, PaymentRecorded, InvoiceOverdue, ...
├── Slips/
│   ├── Commands/        # CreateSlip, AssignSlip, EndAssignment, ...
│   ├── Queries/
│   └── Events/          # SlipAssigned, SlipVacated, ...
├── Customers/
│   ├── Commands/
│   ├── Queries/
│   └── Events/
├── Maintenance/
│   ├── Commands/
│   ├── Queries/
│   └── Events/
├── Notifications/       # Subscribes to events from other modules; no other module depends on it
│   └── Handlers/
└── Announcements/
    ├── Commands/
    └── Queries/
```

### Module rules

- Modules communicate **only via events published through `IMessageBus`** — never by calling each other's handlers directly
- The `Notifications` module is a pure subscriber; nothing depends on it
- Cross-module queries (e.g., billing needs customer name) go through a shared read model or a dedicated query handler, not by importing another module's internals
- If a module's event/query load requires independent scaling, it can be extracted to its own process using the same `IMessageBus` abstraction — zero application code changes

### When to extract a module to its own service

Extract when there is evidence of a specific problem, not in anticipation of one:

| Signal | Candidate extraction |
| --- | --- |
| Notification queue delays are affecting billing job throughput | `Notifications` → dedicated worker Deployment |
| Payment processing requires PCI-scoped isolation | `Billing` → isolated service |
| Reporting queries saturate the primary DB connection pool | Reporting → read replica or separate service |
| A dedicated team takes ownership of a module | Natural service boundary |

On Kubernetes, a worker is just a second Deployment running the same image with a different entrypoint — no separate repo or codebase required until team structure demands it.

---

## Background Jobs & Messaging

### Hangfire (MVP)

Hangfire with `Hangfire.Redis.StackExchange` provides the job queue for MVP:

- Redis-backed storage — no Postgres polling contention, far better throughput
- Batch job support included (no Hangfire Pro required)
- Recurring jobs (cron) for billing sweeps, overdue invoice detection
- The Hangfire dashboard is exposed at `/jobs` behind platform-operator auth

### IMessageBus abstraction

All inter-module communication and background work goes through `IMessageBus`, defined in `Application` with no infrastructure dependency:

```csharp
// Application/Abstractions/IMessageBus.cs
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

**Future implementation:** `NatsMessageBus` — publishes to NATS JetStream subjects. Consumers run in a dedicated worker process or a separate Kubernetes Deployment, subscribed to the same subjects.

### NATS JetStream (future)

When the system needs true pub/sub, fan-out to multiple consumers, or event replay:

- Runs as a lightweight StatefulSet in the cluster
- JetStream provides durable, persistent streams with consumer groups and replay from offset
- Swapping `HangfireMessageBus` for `NatsMessageBus` requires only a DI registration change
- Enables an event-sourcing or event-carried state transfer pattern for inter-module communication at scale

### Redis

Redis is a first-class infrastructure dependency (not optional):

- Hangfire job storage (via `Hangfire.Redis.StackExchange`)
- SignalR scale-out backplane — required for SignalR to work correctly across multiple API pods
- General-purpose cache (output caching, distributed cache via `IDistributedCache`)

Cloud-agnostic: self-hosted in-cluster for dev, or managed (AWS ElastiCache, Azure Cache for Redis, Upstash) in production.

---

## API Design

- **Controller-based API** (`[ApiController]` on every controller) — more readable than Minimal APIs for a complex multi-resource surface; attribute-based documentation of response codes, headers, and models is clearer and more maintainable
- **OpenAPI spec** auto-generated via `Microsoft.AspNetCore.OpenApi`
- **Scalar** replaces Swagger UI for interactive API docs
- Response types documented explicitly with `[ProducesResponseType]` on every action
- All error responses use **RFC 9457 Problem Details** (`ValidationProblemDetails` for 422, `ProblemDetails` for all others)
- XML doc comments on controllers and DTOs feed into the OpenAPI spec
- Pagination via cursor-based tokens (not offset) for scalability
- Custom headers (e.g., `X-Tenant-Id`, `X-Request-Id`) documented via `[ResponseHeader]` attributes

---

## Deployment Architecture

```text
Internet
    │
    ▼
[nginx-ingress / Traefik]      ← TLS termination via cert-manager + Let's Encrypt
    │
    ├──▶ /api/*  →  [MyMarina.Api Pod(s)]   ← HPA for autoscaling
    │                     │
    │                     ├──▶ [PostgreSQL]  ← Managed in prod; in-cluster for dev
    │                     │
    │                     └──▶ [Redis]       ← Hangfire jobs, SignalR, cache
    │                                           Managed in prod; in-cluster for dev
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
- The API (with hot reload via `dotnet watch`)
- The Web app (Vite dev server with HMR)

No Kubernetes required for day-to-day development.

---

## Repository Strategy

Single git repository (monorepo), two Docker build artifacts.

- The `.sln` file excludes `MyMarina.Web` — it is a pure Node project
- GitHub Actions has separate workflow files for API and Web, triggered by path filters
- Both images are pushed to GitHub Container Registry (`ghcr.io`)
- Kubernetes manifests live in `/k8s/` and are applied by the deploy step

Splitting into separate repos is possible later if team structure demands it, but the monorepo removes coordination overhead for full-stack changes during the MVP phase.
