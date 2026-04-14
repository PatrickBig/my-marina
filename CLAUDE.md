# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

**MyMarina** — SaaS marina management platform (mymarina.org).  
See `docs/` for full planning documentation before making significant decisions.

## Status

Pre-scaffold. No code exists yet. Planning documentation is in `docs/`.

| Doc | Purpose |
| --- | --- |
| `docs/overview.md` | Vision, personas, MVP scope, tenant routing strategy |
| `docs/tech-stack.md` | All technology choices and rationale |
| `docs/architecture.md` | Solution structure, multi-tenancy, CQRS pattern, deployment |
| `docs/data-model.md` | All entities, fields, and relationships |
| `docs/roadmap.md` | Phased build order and post-MVP backlog |
| `docs/features/platform-operators.md` | Platform operator feature breakdown |
| `docs/features/marina-operators.md` | Marina operator feature breakdown |
| `docs/features/marina-customers.md` | Marina customer feature breakdown |

## Key Decisions (read before writing code)

- **No MediatR.** Use the explicit `ICommandHandler<T>` / `IQueryHandler<T, TResult>` interface pattern. Cross-cutting concerns via Scrutor decorators.
- **Multi-tenancy:** Shared DB, shared schema, EF Core global query filters. `TenantId` on every tenant-scoped entity. Resolved from JWT claim via `ITenantResolver`.
- **Frontend:** React 19 + TypeScript + Vite. Not a .NET project — lives in `src/MyMarina.Web/` but is excluded from the `.sln`.
- **API types:** OpenAPI spec generated from the ASP.NET backend; TypeScript types generated via `openapi-typescript`. This is the source of truth for the API contract.
- **Payments:** Manual recording only in MVP. `Payment` entity has `PaymentProviderId` / `PaymentProviderReference` fields reserved for future provider integration.

## Build / Test Commands

> To be filled in once the solution is scaffolded (Phase 1).

```bash
# API
dotnet build
dotnet test
dotnet watch --project src/MyMarina.Api

# Frontend
cd src/MyMarina.Web
npm install
npm run dev
npm run build

# Local dev (all services)
docker-compose up
```

## Architecture Overview

> See `docs/architecture.md` for full details.

```text
src/
  MyMarina.Domain/          # Entities, value objects, enums — no dependencies
  MyMarina.Application/     # Commands, queries, handler interfaces, DTOs, validators
  MyMarina.Infrastructure/  # EF Core, Postgres, external services, background jobs
  MyMarina.Api/             # Controller-based API endpoints, middleware, auth, OpenAPI
  MyMarina.Web/             # React/Vite SPA (not in .sln)
tests/
  MyMarina.UnitTests/
  MyMarina.IntegrationTests/  # Uses Testcontainers for real Postgres
k8s/                          # Kubernetes manifests
```

## Tech Stack

- **Backend:** ASP.NET Core (.NET 10), EF Core, PostgreSQL, Hangfire, SignalR, FluentValidation, Scrutor
- **Frontend:** React 19, TypeScript, Vite, TanStack Router, TanStack Query, shadcn/ui, Tailwind CSS v4, Zustand
- **Infra:** Kubernetes, Docker, nginx-ingress, cert-manager, GitHub Actions, ghcr.io
