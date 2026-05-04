# MyMarina — Tech Stack

## Backend — ASP.NET Core (.NET 10)

| Concern | Choice | Rationale |
| --- | --- | --- |
| API style | REST + OpenAPI (Scalar UI) | Well-documented, mobile-ready, broad tooling support |
| API surface | Controller-based (`[ApiController]`) | More readable for complex APIs; richer attribute-based documentation |
| Architecture | Clean Architecture + Vertical Slices (Modular Monolith) | Features stay self-contained; layers are explicit |
| CQRS | Hand-rolled handler interfaces | No external dependency; fully typed; **MediatR avoided intentionally** (commercial license change) |
| ORM | Entity Framework Core 10 | First-class .NET; permission-derived global query filters |
| Database | PostgreSQL | Open source, robust, excellent EF Core driver |
| Identity | ASP.NET Core Identity (`UserManager`, `SignInManager`) | Password hashing, lockout, email confirmation, 2FA scaffolding |
| Auth tokens | Custom JWT issuance | Custom `AuthController` over Identity primitives — **`MapIdentityApi` rejected** because it can't carry our membership claims, can't extend to social login cleanly, can't customize registration logic |
| Social login | `Microsoft.AspNetCore.Authentication.Google` / `.Apple` / `.Facebook` | Standard external-login flow into Identity |
| Background jobs | Hangfire + `Hangfire.Redis.StackExchange` | Redis-backed queue: open source, batch support, far better throughput than Postgres-backed |
| Cache / job store | Redis (StackExchange.Redis) | Hangfire job storage; SignalR backplane for multi-pod; general caching |
| Message bus | `IMessageBus` abstraction | Decouples producers from consumers; Hangfire-backed in MVP, swappable to NATS JetStream |
| Streaming | NATS JetStream *(future)* | Cloud-agnostic, Kubernetes-native event streaming when pub/sub and replay are needed |
| File storage | `IStorageService` abstraction | Swap local → Azure Blob → S3 without touching app code |
| Notifications | `INotificationService` abstraction | Start with SMTP; add SendGrid/Twilio later |
| Real-time | SignalR | In-browser notifications; Redis backplane keeps it working across multiple API pods |
| Validation | FluentValidation | Expressive, easy to test, integrates cleanly with controller actions |
| Dependency injection | Microsoft.Extensions.DI + Scrutor | Scrutor enables decorator pattern for cross-cutting concerns |
| Geo / search (MVP) | Bounding-box query in pure Postgres + Haversine refinement | No PostGIS dependency; sufficient at MVP scale; PostGIS is a future upgrade target |
| Demo enforcement | `WriteAccess` policy decorator on non-GET endpoints | Returns 403 when `IUserContext.IsDemo` is true; one central enforcement point |
| Testing | xUnit + Testcontainers (Postgres) | Integration tests against a real database |

### Why custom JWT issuance vs. `MapIdentityApi`

`MapIdentityApi<T>()` is .NET 8+'s built-in auth endpoint set. It's opinionated:

- Issues opaque bearer tokens (cookie-style), not JWTs. Customizing the format is unsupported.
- Can't embed custom claims — our model needs `memberships` and `billing_accounts` baked in.
- Doesn't extend cleanly to social login (Google, Apple, Facebook).
- No hooks for terms acceptance, marketing opt-in, or post-signup workflows.
- Refresh-token rotation is fixed.

We use Identity's primitives (`UserManager<ApplicationUser>`, `SignInManager<ApplicationUser>`) underneath custom controllers in `MyMarina.Api/Controllers/AuthController.cs`. See [auth-and-permissions.md](./auth-and-permissions.md) for the full design.

### Message Bus Abstraction

The `IMessageBus` and `IMessageHandler<T>` interfaces are defined in `Application` with no infrastructure dependency. In MVP, Hangfire provides the backing implementation. When throughput or streaming demands change, NATS JetStream substitutes without touching application code.

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

**MVP:** `HangfireMessageBus : IMessageBus` — jobs enqueued into Redis-backed Hangfire.
**Future:** `NatsMessageBus : IMessageBus` — publishes to NATS JetStream subjects; consumers in a dedicated worker process or separate Deployment.

### Why Hangfire.Redis.StackExchange over Hangfire Pro

`Hangfire.Redis.StackExchange` is open source (MIT) and provides:

- Redis-backed job storage — orders of magnitude better throughput than Postgres polling
- Batch job support at no cost (Hangfire Pro charges for this on its Postgres/SQL Server backends)
- Uses `StackExchange.Redis`, the standard .NET Redis client

Redis itself is cloud-agnostic: self-hosted, AWS ElastiCache, Azure Cache for Redis, Upstash.

### NATS JetStream (future streaming)

When pub/sub, fan-out, or event replay are needed:

- Cloud-native and Kubernetes-native — runs as a lightweight StatefulSet
- JetStream adds durable, persistent streams on top of NATS core
- `NATS.Net` is the official .NET client (maintained by the NATS team)
- Supports consumer groups, message acknowledgement, replay from offset

---

### CQRS Without MediatR

MediatR is excluded due to its commercial license change. Explicit, typed handler interfaces:

```csharp
public interface ICommandHandler<TCommand>
{
    Task HandleAsync(TCommand command, CancellationToken ct = default);
}

public interface IQueryHandler<TQuery, TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken ct = default);
}
```

Cross-cutting concerns (logging, validation, authorization checks) are added via **Scrutor decorators** registered in DI — no pipeline magic, fully visible, fully typed.

Handlers are auto-registered by scanning assemblies for implementations of the handler interfaces.

---

## Frontend — React (TypeScript)

| Concern | Choice | Rationale |
| --- | --- | --- |
| Framework | React 19 + TypeScript | Large ecosystem; effective with Claude Code |
| Build tool | Vite | Fast builds, great DX |
| Routing | TanStack Router | Type-safe routes, file-based routing |
| Server state | TanStack Query | Caching, background refetch, optimistic updates |
| Component library | shadcn/ui + Radix UI | Accessible primitives, copy-paste model, Tailwind-native |
| Styling | Tailwind CSS v4 | Pairs with shadcn; utility-first |
| Forms | React Hook Form + Zod | Validation schema mirrors backend rules |
| Client state | Zustand | Lightweight, no boilerplate |
| API types | openapi-typescript (codegen from OpenAPI spec) | Frontend types stay in sync with backend automatically |
| HTTP client | Axios with TanStack Query | Bearer-token interceptor, retry logic |
| Maps | Leaflet + OpenStreetMap tiles | No API key required; sufficient for marketplace map view |
| Date/time | date-fns or Day.js | TBD at scaffold time |

---

## Marketing Site

| Concern | Choice | Rationale |
| --- | --- | --- |
| Framework | Astro (static) | Fast, SEO-friendly, low overhead |
| Hosting | Same K8s cluster | Static bundle served via separate Deployment |
| Screenshots | Captured via Playwright from the live demo | Stay current automatically when UI ships |
| CTA targets | Marketing → demo (auto-signin) and signup flows | Hand off to the API |

---

## Infrastructure & Deployment

| Concern | Choice | Rationale |
| --- | --- | --- |
| Containerization | Docker (separate images for API, Web, Marketing) | Each service is independently deployable |
| Orchestration | Kubernetes | Cloud-provider agnostic |
| Ingress | nginx-ingress or Traefik | Avoid cloud-specific load balancers |
| TLS | cert-manager + Let's Encrypt | Automated certificate management |
| Config / secrets | Kubernetes Secrets + env vars | No appsettings.json at runtime; 12-factor app |
| Database hosting | Managed Postgres (cloud) or in-cluster for dev | Cloud-agnostic |
| Cache / queue store | Managed Redis (cloud) or in-cluster for dev | Cloud-agnostic |
| CI/CD | GitHub Actions | Build, test, push images, deploy to cluster |
| Image registry | GitHub Container Registry (ghcr.io) | Free for public/private, integrated with Actions |
| Error tracking (post-MVP) | Sentry or similar | Centralized error logs across pods |

---

## Future: Stripe Connect (Era 2)

When online payments land:

| Concern | Choice |
| --- | --- |
| Payment processor | Stripe Connect (Express accounts for hosts) |
| Boater payment methods | Stripe Customer + PaymentMethod |
| Webhooks | `/webhooks/stripe` endpoint with signature verification |
| Compliance | KYC/AML via Stripe Connect onboarding |

The data model carries placeholders (`PaymentIntentId`, `PlatformFeeAmount`, `PaymentStatus`, `RevenueSplitSnapshot`) so this lands additively.

---

## API Contract Management

The OpenAPI spec generated by the ASP.NET Core backend is the source of truth for the API contract. The frontend consumes it via `openapi-typescript` to auto-generate TypeScript types, eliminating frontend/backend drift.

A shared `openapi.json` artifact is produced in CI and versioned alongside the code.
