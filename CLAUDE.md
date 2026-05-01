# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

**MyMarina** — SaaS marina management + two-sided slip marketplace (mymarina.org).  
See `docs/` for full planning documentation before making significant decisions.

## Status

**Marketplace redesign complete (docs). v0 codebase (Phases 1–4) is superseded.**  
The design was overhauled in a docs-only session: global vessels, marina-pinned slip ownership, Membership-based auth, single IUserContext, and a static demo tenant. All docs below reflect the new model.

Next: Phase 0 — Hard reset: strip out v0 code that conflicts with the new model; establish the new entity graph, auth system, and IUserContext before feature work resumes.

| Doc | Purpose |
| --- | --- |
| `docs/overview.md` | Vision, personas, MVP scope, tenant routing strategy |
| `docs/tech-stack.md` | All technology choices and rationale |
| `docs/architecture.md` | Solution structure, multi-tenancy, CQRS pattern, IUserContext, demo experience |
| `docs/data-model.md` | All entities, fields, and relationships |
| `docs/auth-and-permissions.md` | JWT design, social login, authorization policies, refresh token rotation |
| `docs/roadmap.md` | Phased build order and post-MVP backlog |
| `docs/glossary.md` | Terminology; v0 → new name mapping |
| `docs/vessels.md` | User-scoped vessels, MarinaVesselRecord overlay, ghost vessel claim flow |
| `docs/marketplace.md` | Search algorithm, AvailabilityWindow, reservation lifecycle, sublet flows |
| `docs/features/platform-operators.md` | Platform operator feature breakdown |
| `docs/features/marina-operators.md` | Marina operator feature breakdown |
| `docs/features/boaters.md` | Boater feature breakdown (replaces marina-customers.md) |
| `docs/features/private-slip-owners.md` | Private dock / dockominium owner feature breakdown |

## Key Decisions (read before writing code)

- **No MediatR.** Use `ICommandHandler<T>` / `IQueryHandler<T, TResult>` from `MyMarina.Application.Abstractions`. Cross-cutting concerns via Scrutor decorators.

- **Global user identity:** `ApplicationUser` has no `TenantId`. A user's permissions derive entirely from `Membership` records (User → Marina or Tenant, with role) and `BillingAccountMember` records (User → BillingAccount, with role). There is no `UserContext` junction table. No re-login or context switching required — all roles are embedded in the JWT at login.

- **IUserContext:** Single per-request abstraction replacing v0's `ITenantContext` / `IMarinaContext` / `ICustomerContext`. Populated from JWT claims by `HttpUserContext` in Infrastructure. Key properties: `UserId`, `Email`, `IsPlatformOperator`, `Memberships` (list of `{MarinaId?, TenantId, Role}`), `BillingAccounts` (list of `{BillingAccountId, MarinaId, Role}`), `IsDemo`. Helpers: `HasMarinaAccess(marinaId)`, `HasTenantAccess(tenantId)`. Register as `IUserContext` via Scrutor.

- **Multi-tenancy:** Shared DB, shared schema, EF Core global query filters on `TenantId`. `IUserContext.IsPlatformOperator` bypasses tenant filters. Marina-level handlers additionally filter by `MarinaId` where applicable. There is no `ITenantContext` / `IMarinaContext` / `ICustomerContext` — `IUserContext` is the only context interface.

- **Marina-pinned slip ownership:** All slips belong to a `Marina` (`Slip.MarinaId` always set). Private dock owners and dockominium owners get auto-provisioned Free-tier `Tenant` + single-slip `Marina` (`MarinaType = PrivateDock` or `Dockominium`). There is no `Slip.OwnerUserId` or `Slip.OwnerKind`. All slip permissions resolve through `Membership` at `Slip.MarinaId`.

- **Vessel vs Boat naming:** The canonical entity, API contract, and database table are always `Vessel`. The UI, marketing copy, and user-facing strings use "boat." Never add a `Boat` entity or `boat_id` column. `MarinaVesselRecord` is the per-marina overlay (alias, notes, slip assignment links); it does not own the vessel.

- **Identity entities in Infrastructure:** `ApplicationUser` and `ApplicationRole` live in `MyMarina.Infrastructure.Identity` (not Domain) because they depend on ASP.NET Core Identity. Domain entities reference users by `UserId` (Guid) only.

- **Primary keys:** UUID v7 (`Guid.CreateVersion7()`) — time-ordered, B-tree friendly, globally unique.

- **Billing accounts:** `BillingAccount` (not `CustomerAccount` / `Customer`) — multiple `BillingAccountMember` records per account with Owner/CoOwner/Member roles. A `BillingAccount` belongs to a Marina. Long-term slip leases link a `BillingAccount` to a `SlipAssignment`; `BillingAccountId` is required on `SlipAssignment` (no nullable owner kind polymorphism).

- **Moorings:** `Slip.DockId` is nullable — null = free-standing mooring/anchorage. `Slip.MarinaId` is always set.

- **JWT claim shape:** `sub`, `email`, optional `platform_role`, `memberships` (JSON array — each entry: `{tenantId, marinaId?, role}`), `billing_accounts` (JSON array — each entry: `{billingAccountId, marinaId, role}`), `is_demo` (boolean). No `marina_id` / `tenant_id` at top-level. No `has_multiple_contexts`. See `docs/auth-and-permissions.md` for full shape and size analysis.

- **Social login:** `Microsoft.AspNetCore.Authentication.Google` / `.Apple` / `.Facebook` via standard external-login flow into Identity. `MapIdentityApi<T>()` is **intentionally rejected** — it cannot carry custom claims, cannot extend to social login, and cannot hook post-signup workflows. See `docs/auth-and-permissions.md` → "Why custom JWT issuance."

- **Demo tenant:** A single `Tenant.IsDemo = true` static tenant seeded by `DemoSeedScript.SeedAsync`. The `WriteAccess` authorization policy decorator blocks all non-GET endpoints when `IUserContext.IsDemo = true` (returns 403). Demo listings are excluded from real-user marketplace search. Anonymous demo sessions use a short-lived `is_demo = true` JWT. See `docs/architecture.md` → "Demo Experience."

- **Demo seed script is a living artifact:** `DemoSeedScript.SeedAsync` in `MyMarina.Infrastructure` must be updated in the **same PR** as any change that adds a new entity type or major capability. It provisions the single demo tenant with rich data covering every capability the platform currently supports. A CI integration test asserts at least one record exists per known entity type — a failing seed breaks the build.

- **Frontend:** React 19 + TypeScript + Vite. Not a .NET project — lives in `src/MyMarina.Web/`, excluded from `.sln`.

- **API types:** OpenAPI spec (auto-generated by ASP.NET Core) → `npm run generate-api` → `src/api/schema.d.ts`. Run after backend changes.

- **Frontend API types:** NEVER manually edit `src/MyMarina.Web/src/api/schema.d.ts`. It is auto-generated by `npm run generate-api` (openapi-typescript from the running API). After any backend contract change, run the API, then regenerate. Manually editing it causes it to drift from the real OpenAPI spec and breaks the codegen workflow.

- **Payments:** Era 1 (MVP) = manual/off-platform only. `Payment.PaymentStatus = OffPlatform`. `Payment.PaymentProviderId` and `Payment.PaymentProviderReference` are reserved for Era 2. Era 2 = Stripe Connect; `RevenueSplitSnapshot` on `Reservation` drives automated payouts.

- **EF migration workflow:** NEVER manually edit migration files or `AppDbContextModelSnapshot.cs`. Always use `dotnet ef migrations add <Name>` to generate migrations and let EF own the output entirely. If a migration looks wrong, fix the entity/configuration and regenerate — do not patch the generated file. The only exception is adding a SQL `migrationBuilder.Sql(...)` call for data backfills that EF cannot express, but the structural `AddColumn`/`DropColumn`/`CreateTable` calls must always be EF-generated.

- **Subscription tiers:** `Tenant.SubscriptionTier` (enum: `Free=0`, `Pro=1`, `Premium=2`) gates feature access. Use `[RequiresTier(SubscriptionTier.X)]` on controller actions to enforce tier requirements. Embed `subscription_tier` in the JWT via `JwtTokenService`. **Specific feature-to-tier assignments live in `TierCapabilityRegistry` (`MyMarina.Infrastructure/Demo/TierCapabilityRegistry.cs`) — this is a living document. When a new feature ships, add a capability constant and assign it to the appropriate tier(s) in the same PR. Free tier is intentionally very limited.**

- **Marketing site screenshots stay current via Playwright:** Use the `playwright-cli` skill to capture fresh screenshots of the running SaaS app whenever a UI feature ships. Screenshots live in `src/MyMarina.Marketing/public/screenshots/` and are referenced by `ScreenshotsSection`. When a new phase adds visible UI, capture updated shots and commit them in the same PR.

- This product is NOT LIVE in production yet. It is perfectly acceptable to perform destructive database schema changes.

## Build / Test Commands

```bash
# API — from repo root
dotnet build
dotnet test
dotnet watch --project src/MyMarina.Api

# EF Core migrations (requires Postgres running)
dotnet ef migrations add <Name> --project src/MyMarina.Infrastructure --startup-project src/MyMarina.Api
dotnet ef database update --project src/MyMarina.Infrastructure --startup-project src/MyMarina.Api

# Frontend — from src/MyMarina.Web/
npm install
npm run dev          # Vite dev server on :5173, proxies /api → :5000
npm run build        # Production build
npm run generate-api # Regenerate TypeScript types from running API OpenAPI spec

# Local dev (all services via Docker Compose)
docker-compose up
```

## Architecture Overview

> See `docs/architecture.md` for full details.

```text
src/
  MyMarina.Domain/          # Entities, value objects, enums — no dependencies
  MyMarina.Application/     # Handler interfaces, abstractions (ICommandHandler, IQueryHandler, IMessageBus, etc.)
  MyMarina.Infrastructure/  # EF Core, Postgres, Identity, Hangfire+Redis, IUserContext
    Identity/               # ApplicationUser, ApplicationRole (ASP.NET Core Identity)
    Persistence/            # AppDbContext, EF configurations, migrations
    UserContext/            # HttpUserContext (implements IUserContext — resolves from JWT claims)
    Messaging/              # HangfireMessageBus (IMessageBus implementation)
    Demo/                   # DemoSeedScript, TierCapabilityRegistry, WriteAccess policy decorator
  MyMarina.Api/             # Controller-based API endpoints, middleware, auth, OpenAPI, Scalar UI
    Controllers/            # API controllers (one per feature area)
    Infrastructure/         # HangfireAuthFilter, WriteAccess filter, etc.
  MyMarina.Web/             # React 19 / Vite SPA (not in .sln)
    src/api/                # client.ts (Axios) + schema.d.ts (OpenAPI codegen output)
  MyMarina.Marketing/       # Astro static marketing site
tests/
  MyMarina.UnitTests/       # Domain logic, application handler unit tests
  MyMarina.IntegrationTests/ # Full HTTP stack via WebApplicationFactory + Testcontainers (real Postgres)
k8s/                        # Kubernetes manifests (to be added in CI/CD phase)
```

### Module breakdown (within Application / Infrastructure)

| Module | Responsibility |
| --- | --- |
| Identity | Auth, JWT issuance, social login, refresh tokens |
| Marinas | Tenant + Marina CRUD, MarinaType, onboarding wizards |
| Memberships | Membership CRUD, role management, invitation flows |
| Vessels | Vessel CRUD, MarinaVesselRecord overlay, ghost vessel claim |
| BillingAccounts | BillingAccount + BillingAccountMember, invoice, payment recording |
| Marketplace | AvailabilityWindow, slip search (bounding-box + vessel-fit), listing moderation |
| Assignments | SlipAssignment, "I'm Away," sublet policy enforcement |
| Reservations | Reservation lifecycle, HostMarinaPolicy approval flow, cancellation |
| Maintenance | MaintenanceRequest, WorkOrder |
| Announcements | Announcement, audience targeting |
| Notifications | INotificationService, email dispatch |
| Platform | Platform-operator actions, audit log, demo tenant management |

## Tech Stack

- **Backend:** ASP.NET Core (.NET 10), EF Core 10, PostgreSQL, Hangfire + Hangfire.Redis.StackExchange, SignalR, FluentValidation, Scrutor
- **Frontend:** React 19, TypeScript 6, Vite 8, TanStack Router, TanStack Query, shadcn/ui, Tailwind CSS v4, Zustand, Zod, React Hook Form
- **Infra:** Kubernetes, Docker, nginx-ingress, cert-manager, GitHub Actions, ghcr.io
