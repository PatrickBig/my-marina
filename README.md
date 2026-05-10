# MyMarina

**SaaS marina management + two-sided slip marketplace** — like Airbnb for dockage.

[![.NET 10](https://img.shields.io/badge/.NET-10-blue)](https://dotnet.microsoft.com)
[![React 19](https://img.shields.io/badge/React-19-61DAFB)](https://react.dev)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-17-336791)](https://www.postgresql.org)
[![License](https://img.shields.io/badge/license-Proprietary-red)]()

---

## Overview

MyMarina connects boaters with available dockage across a network of marinas and private slip owners. Two value propositions in one platform:

- **Marketplace for boaters** — discover slips, reserve dockage, show up. One account works at every participating marina.
- **SaaS for marinas** — manage leases, invoicing, customers, maintenance, and staff without spreadsheets.

Both sides reinforce each other. Marinas get operational software plus incremental revenue from transient guests. Boaters get a single identity that travels with them.

**Domain:** [mymarina.org](https://mymarina.org)

---

## Revenue Model

| Era | How it works | Status |
|-----|-------------|--------|
| **Era 1 — SaaS subscriptions** | Marinas pay Free / Pro / Premium tiers. Boater accounts are free. Reservations facilitated end-to-end; payments off-platform (manual invoicing). | MVP |
| **Era 2 — Transaction fees** | Payments flow through the platform (Stripe Connect). Platform deducts a fee, pays out to hosts. | Planned |

The MVP data model is built for Era 2 from day one — `PaymentIntentId`, `PlatformFeeAmount`, `RevenueSplitSnapshot` are reserved fields on `Reservation`.

---

## The Three Personas

### Boaters
Search slips near a destination, filter by boat dimensions, reserve transient or seasonal dockage, manage their global vessel profile, track invoices and maintenance requests across all marinas. No per-marina sign-ups.

### Marina Hosts
Commercial marinas and yacht clubs manage docks, slips, long-term leases, staff memberships, invoicing, announcements, and maintenance. List on the marketplace for transient revenue.

### Private Slip Owners
Individual dock/dockominium owners list their slips peer-to-peer. Auto-provisioned Free-tier tenant + single-slip marina behind the scenes. UX says "your dock," not "marina."

<details>
<summary><strong>Platform Operators</strong> (click to expand)</summary>

MyMarina staff provision tenants, configure tiers, moderate listings, handle access escalations with full audit trail.

</details>

---

## Architecture

```
src/
  MyMarina.Domain/          # Entities, value objects, enums — zero dependencies
  MyMarina.Application/     # CQRS handler interfaces, abstractions, DTOs, validators
  MyMarina.Infrastructure/  # EF Core, Identity, Hangfire+Redis, IUserContext
  MyMarina.Api/             # Controller-based API, auth, OpenAPI, Scalar UI
  MyMarina.Web/             # React 19 / Vite SPA
  MyMarina.Marketing/       # Astro static site

tests/
  MyMarina.UnitTests/       # Domain + application unit tests
  MyMarina.IntegrationTests/ # HTTP stack + real Postgres via Testcontainers
```

**Clean Architecture + Vertical Slices** (Modular Monolith). No MediatR — explicit `ICommandHandler<T>` / `IQueryHandler<T, TResult>` with Scrutor decorators for cross-cutting concerns.

### Key Design Decisions

| Decision | Choice | Why |
|---------|--------|-----|
| **Identity** | No `TenantId` on `User` | Users are first-class platform citizens, not tenant-bound |
| **Auth** | Custom JWT over Identity (`MapIdentityApi` rejected) | Custom claims, social login support, post-signup hooks |
| **Multi-tenancy** | EF Core global query filters | Permission-derived row-level access, no context switching |
| **CQRS** | Hand-rolled handlers + Scrutor decorators | No licensing risk, fully typed, composable decorators |
| **Geo search** | Bounding-box + Haversine | No PostGIS dependency at MVP; upgrade target later |
| **Background jobs** | Hangfire + Redis | Open source, batch support, better than Postgres polling |
| **Message bus** | `IMessageBus` abstraction | MVP uses Hangfire; swap to NATS JetStream later |
| **Primary Keys** | UUID v7 | Time-ordered, B-tree friendly, globally unique |

### Identity Model

A user has **no fixed role**. Permissions come from two independent junction tables:

| Junction | Grants |
|----------|--------|
| `Membership` (User → Marina or Tenant) | Host-side permissions (Owner, Manager, Staff) |
| `BillingAccountMember` (User → BillingAccount) | Customer-side permissions at a marina |

Sign in once. See everything you have access to. No role toggle. No context switch.

See the [auth docs](./docs/auth-and-permissions.md) for the JWT claim shape.

---

## Marketplace Uniqueness

Three sources of marketplace availability from a single slip:

1. **Owner-direct** — the slip's marina lists it
2. **Holder sublet** — the long-term lease holder lists while away
3. **Owner sublet of leased slip** — the marina lists during the holder's absence

Lease agreements negotiate revenue splits for each direction. This three-source model is a deliberate differentiator vs. existing marina management software.

Dockominium slips (individual-owned within a physical marina) have a configurable host-marina approval policy: `None | NotifyOnly | RequiresApproval`.

---

## Tech Stack

**Backend:** ASP.NET Core 10 · EF Core 10 · PostgreSQL 17 · Redis · Hangfire · SignalR · FluentValidation · Scrutor

**Frontend:** React 19 · TypeScript 6 · Vite · TanStack Router · TanStack Query · shadcn/ui · Tailwind CSS v4 · Zustand · Zod · React Hook Form

**Infra:** Docker · Kubernetes (Helm) · GitHub Actions · ghcr.io · nginx-ingress + cert-manager

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js 20+](https://nodejs.org)
- [Docker](https://docker.com)

### Quick Start

```bash
# Spin up everything (Postgres + Redis + API + Web + Marketing)
docker-compose up

# Or develop services in parallel
make dev-api     # API with dotnet watch
make dev-web     # React dev server on :5173
make dev-marketing  # Marketing site on :4321
```

### Manual Setup

```bash
# API
cd src/MyMarina.Api
dotnet restore
dotnet run

# Web
cd src/MyMarina.Web
npm install
npm run dev
```

### Database Migrations

```bash
dotnet ef migrations add <Name> \
  --project src/MyMarina.Infrastructure \
  --startup-project src/MyMarina.Api
```

### Running Tests

```bash
dotnet build
dotnet test
```

Integration tests use Testcontainers with a real Postgres database.

---

## Documentation

| Doc | Purpose |
|-----|---------|
| [Project Overview](./docs/overview.md) | Vision, personas, revenue model, MVP scope |
| [Architecture](./docs/architecture.md) | Solution structure, CQRS, modular monolith, demo experience |
| [Data Model](./docs/data-model.md) | Entities, fields, relationships |
| [Auth & Permissions](./docs/auth-and-permissions.md) | JWT design, social login, authorization policies |
| [Tech Stack](./docs/tech-stack.md) | All technology choices and rationale |
| [Marketplace](./docs/marketplace.md) | Search algorithm, listing, reservation lifecycle, sublet flows |
| [Vessels](./docs/vessels.md) | User-scoped vessels, ghost vessel claim flow |
| [Glossary](./docs/glossary.md) | Terminology reference |
| [Roadmap](./docs/roadmap.md) | Phased build order |
| [Boaters](./docs/features/boaters.md) | Boater feature breakdown |
| [Marina Operators](./docs/features/marina-operators.md) | Marina operator feature breakdown |
| [Platform Operators](./docs/features/platform-operators.md) | Platform operator feature breakdown |
| [Private Slip Owners](./docs/features/private-slip-owners.md) | Private dock / dockominium features |

---

## Deployment

```
Internet
    │
    ▼
[nginx-ingress / Traefik]        ← TLS via cert-manager + Let's Encrypt
    │
    ├──▶ /api/*  →  [MyMarina.Api Pod(s)]    ← HPA autoscaling
    │                      │
    │                      ├──▶ [PostgreSQL]
    │                      │
    │                      └──▶ [Redis]
    │
    └──▶ /*      →  [MyMarina.Web Pod(s)]    ← Static React bundle
```

Kubernetes manifests in [`charts/`](./charts/) and [`k8s/`](./k8s/). CI/CD via GitHub Actions in [`.github/workflows/`](.github/workflows/).
