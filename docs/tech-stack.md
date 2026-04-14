# MyMarina — Tech Stack

## Backend — ASP.NET Core (.NET 10)

| Concern | Choice | Rationale |
| --------- | -------- | ----------- |
| API style | REST + OpenAPI (Scalar UI) | Well-documented, mobile-ready, broad tooling support |
| API surface | Controller-based (`[ApiController]`) | More readable for complex APIs; richer attribute-based documentation of response codes, headers, and models |
| Architecture | Clean Architecture + Vertical Slices | Features stay self-contained; layers are explicit |
| CQRS | Hand-rolled handler interfaces (see below) | No external dependency; fully typed; MediatR avoided intentionally |
| ORM | Entity Framework Core 10 | First-class .NET, global query filters for multi-tenancy |
| Database | PostgreSQL | Open source, robust, excellent EF Core driver |
| Auth | ASP.NET Core Identity + JWT Bearer | Well-understood, extensible to OIDC/SSO later |
| Background jobs | Hangfire + `Hangfire.Redis.StackExchange` | Redis-backed queue: no Hangfire Pro needed, includes batch support, far better throughput than Postgres-backed storage |
| Cache / job store | Redis (StackExchange.Redis) | Hangfire job storage; SignalR backplane for multi-pod; general caching |
| Message bus | `IMessageBus` abstraction (see below) | Decouples producers from consumers; backed by Hangfire in MVP, swappable to NATS JetStream or RabbitMQ |
| Streaming | NATS JetStream *(future)* | Cloud-agnostic, Kubernetes-native event streaming when pub/sub and replay are needed |
| File storage | `IStorageService` abstraction | Swap local → Azure Blob → S3 without touching app code |
| Notifications | `INotificationService` abstraction | Start with SMTP; add SendGrid/Twilio later |
| Real-time | SignalR | In-browser notifications; Redis backplane keeps it working across multiple API pods |
| Validation | FluentValidation | Expressive, easy to test, integrates cleanly with controller actions |
| Dependency injection | Microsoft.Extensions.DI + Scrutor | Scrutor enables decorator pattern for cross-cutting concerns |
| Testing | xUnit + Testcontainers (Postgres) | Integration tests against a real database |

### Message Bus Abstraction

The `IMessageBus` and `IMessageHandler<T>` interfaces are defined in `Application` with no infrastructure dependency. In MVP, Hangfire provides the backing implementation. When throughput or streaming needs demand it, NATS JetStream or RabbitMQ can be substituted without touching any application code.

```csharp
// Application layer — no infrastructure dependency
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

**MVP:** `HangfireMessageBus : IMessageBus` — jobs enqueued into Redis-backed Hangfire queue.  
**Future:** `NatsMessageBus : IMessageBus` — publishes to NATS JetStream subjects; consumers run in a dedicated worker process or separate deployment.

#### Why Hangfire.Redis.StackExchange over Hangfire Pro

`Hangfire.Redis.StackExchange` is open source (MIT) and provides:

- Redis-backed job storage — orders of magnitude better throughput than Postgres polling
- Batch job support at no cost (Hangfire Pro charges for this on its Postgres/SQL Server backends)
- Uses `StackExchange.Redis`, which is already the standard .NET Redis client

Redis itself is cloud-agnostic: self-hosted, AWS ElastiCache, Azure Cache for Redis, Upstash, etc.

#### NATS JetStream (future streaming)

When pub/sub, fan-out, or event replay are needed:

- Cloud-native and Kubernetes-native — runs as a lightweight StatefulSet
- JetStream adds durable, persistent streams on top of NATS core
- `NATS.Net` is the official .NET client (maintained by the NATS team)
- Supports consumer groups, message acknowledgement, replay from offset
- Replaces ad-hoc polling patterns with true event-driven processing

---

### CQRS Without MediatR

MediatR is intentionally excluded due to its commercial license change. Instead, we use a simple, explicit handler interface pattern:

```csharp
// Interfaces
public interface ICommandHandler<TCommand>
{
    Task HandleAsync(TCommand command, CancellationToken ct = default);
}

public interface IQueryHandler<TQuery, TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken ct = default);
}

// Usage in an endpoint or controller
public class CreateSlipEndpoint(ICommandHandler<CreateSlipCommand> handler)
{
    // ...
}
```

Cross-cutting concerns (logging, validation, authorization checks) are added via **Scrutor decorators** registered in DI — no pipeline magic, fully visible, fully typed.

Handlers are auto-registered by scanning assemblies for implementations of the handler interfaces.

---

## Frontend — React (TypeScript)

| Concern | Choice | Rationale |
|---|---|---|
| Framework | React 19 + TypeScript | Large ecosystem; Claude is highly effective with it |
| Build tool | Vite | Fast builds, great DX |
| Routing | TanStack Router | Type-safe routes, file-based routing option |
| Server state | TanStack Query | Caching, background refetch, optimistic updates |
| Component library | shadcn/ui + Radix UI | Accessible primitives, copy-paste model, Tailwind-native |
| Styling | Tailwind CSS v4 | Pairs perfectly with shadcn; utility-first |
| Forms | React Hook Form + Zod | Validation schema can mirror backend rules |
| Client state | Zustand | Lightweight, no boilerplate |
| API types | openapi-typescript (codegen from OpenAPI spec) | Frontend types stay in sync with backend automatically |
| HTTP client | Axios or native fetch with TanStack Query | TBD at scaffold time |

---

## Infrastructure & Deployment

| Concern | Choice | Rationale |
|---|---|---|
| Containerization | Docker (separate images for API and Web) | Each service is independently deployable |
| Orchestration | Kubernetes | Cloud-provider agnostic |
| Ingress | nginx-ingress or Traefik | Avoid cloud-specific load balancers |
| TLS | cert-manager + Let's Encrypt | Automated certificate management |
| Config / secrets | Kubernetes Secrets + env vars | No appsettings.json at runtime; 12-factor app |
| Database hosting | Managed Postgres (cloud) or in-cluster for dev | Cloud-agnostic; Postgres is available everywhere |
| Cache / queue store | Managed Redis (cloud) or in-cluster for dev | Hangfire jobs, SignalR scale-out, general cache; cloud-agnostic |
| CI/CD | GitHub Actions | Build, test, push images, deploy to cluster |
| Image registry | GitHub Container Registry (ghcr.io) | Free for public/private, integrated with Actions |

---

## API Contract Management

The OpenAPI spec generated by the ASP.NET Core backend is the source of truth for the API contract. The frontend consumes it via `openapi-typescript` to auto-generate TypeScript types, eliminating a whole class of frontend/backend drift bugs.

A shared `openapi.json` artifact is produced in CI and versioned alongside the code.
