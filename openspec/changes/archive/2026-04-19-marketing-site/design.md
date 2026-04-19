## Context

MyMarina currently has no public-facing web presence. The SaaS application lives behind authentication and is only accessible once an account exists. Prospects have no way to evaluate the product without a sales touch. This design covers two related pieces: a static marketing site and an interactive demo environment that drops visitors into a pre-seeded, sandboxed version of the real product.

The existing stack is React 19 + Vite + Tailwind CSS v4 + TanStack Router for the SaaS frontend, and ASP.NET Core + EF Core + PostgreSQL for the backend. The demo reuses this entire stack with no new backend capabilities — it is just a seeded tenant with a frictionless login shortcut.

## Goals / Non-Goals

**Goals:**
- Public marketing site that communicates the product's value to marina operators and their customers
- Interactive demo where a visitor clicks "Try Demo" and lands in a realistic, pre-authenticated operator or customer session within seconds, with a full two-marina dataset covering every current capability
- Demo tenants are isolated per visitor, expire after a configurable TTL, and are automatically cleaned up
- Demo tenants are locked to a specific subscription tier, giving prospects a faithful picture of what each plan includes
- Marketing site and SaaS app share the same Tailwind design tokens (no visual divergence)
- Marketing site produces fully server-rendered HTML for search engine and social media crawler indexing

**Non-Goals:**
- CMS-driven content management (static markdown is sufficient for MVP)
- A/B testing or analytics beyond basic page-view tracking
- Separate demo infrastructure (demo runs inside the same API/DB as production, in an isolated tenant)
- Self-service account creation from the marketing site (out of scope for MVP — direct to contact form)

## Decisions

### 1. Marketing site uses Astro (SSG) for SEO-first output

**Decision:** Add `src/MyMarina.Marketing/` as an Astro project. Astro generates fully-rendered static HTML at build time — each page is a complete document that search engine crawlers and social media link scrapers (Facebook Open Graph, Twitter Card) can index without executing JavaScript. Tailwind CSS v4 is used via the `@astrojs/tailwind` integration, sharing the same color/spacing tokens as the SaaS app. React components can be embedded as Astro islands where interactivity is needed (e.g., the contact form, the demo CTA button).

**Alternatives considered:**
- *Vite + React SPA*: Familiar stack, but JS-rendered HTML is invisible to crawlers unless SSR or a prerender plugin is added. SEO is a hard requirement from the proposal, not a nice-to-have.
- *Vite + vite-plugin-prerender*: Generates static snapshots at build time. Simpler than Astro but less mature, brittle for dynamic routes, and gives up Astro's content collection and built-in sitemap/RSS support.
- *External service (Webflow, Framer)*: Fast to launch but divorces the site from the codebase, making it impossible to share design tokens or keep the site in sync with new features as required by the proposal.
- *Single app with a public route*: Mixes marketing concerns into the SaaS app and makes the SaaS bundle larger for authenticated users.

### 2. Demo session via a signed demo JWT endpoint

**Decision:** Add `POST /api/demo/session?role={operator|customer}` which issues a short-lived JWT (30-minute TTL, `is_demo: true` claim) scoped to the demo tenant. No credentials required. The marketing site "Try Demo" button calls this endpoint and redirects to the SaaS app with the token, which is stored in session storage (not localStorage, so it doesn't persist after the tab closes).

**Alternatives considered:**
- *Hard-coded demo credentials published on the site*: Simple but credentials can be scraped, session shared across visitors causing data pollution.
- *OAuth-style demo token in the URL*: Leaks token into browser history and server logs.
- *Fully isolated demo environment (separate deployment)*: Clean isolation but doubles infra and complicates keeping demo data in sync with production schema.

### 3. Per-visitor ephemeral demo tenant

**Decision:** Each "Try Demo" request provisions a brand-new isolated tenant, seeds it with realistic data, issues a JWT scoped to that tenant, and stamps the tenant with a `DemoExpiresAt` timestamp (default TTL: 60 minutes, configurable). A Hangfire recurring job sweeps expired demo tenants every 15 minutes, deleting the tenant and all its data via EF cascade deletes. Because each visitor owns their own tenant, they have full write access to everything — no write guard, no risk of one visitor's actions affecting another's.

**Alternatives considered:**
- *Single shared demo tenant with periodic reset*: Simpler infra, but visitors contaminate each other's sessions between resets; a write guard is needed to prevent structural damage; the reset window creates a dirty-state window.
- *Snapshot/restore at database level*: Clean but requires DBA-level access and doesn't compose with shared-schema multi-tenancy.
- *Fully read-only demo*: Eliminates isolation problems but prevents visitors from experiencing core write flows (booking a slip, recording a payment), which is the point of the demo.

### 4. Tenant lifecycle via `IsDemo` + `DemoExpiresAt` + `SubscriptionTier` columns

**Decision:** `Tenant` gets three new columns: `IsDemo` (boolean, default false), `DemoExpiresAt` (nullable `DateTimeOffset`), and `SubscriptionTier` (enum: `Free=0`, `Pro=1`, `Premium=2`, default `Free`). The demo provisioning command sets all three; the cleanup job queries `WHERE IsDemo = true AND DemoExpiresAt < now()`. A `[RequiresTier(SubscriptionTier.X)]` action filter attribute reads `subscription_tier` from the JWT and returns 403 with a `tier_required` error code, gating Pro/Premium features. This means demo visitors at the `Free` tier experience exactly the same gates a real Free tenant would — no special-casing for demo mode. The `subscription_tier` and `is_demo` claims are embedded in the JWT at provisioning time; the frontend uses them to drive the banner and any upgrade prompts.

**Alternatives considered:**
- *Separate `DemoSession` table*: Cleaner separation of concerns but adds a join on every demo request and complicates cascade deletes.
- *TTL-based deletion without a flag*: Could accidentally delete real tenants if clock skew or logic errors occur; the explicit `IsDemo` flag makes the cleanup query unambiguous.
- *Feature flags (LaunchDarkly / custom)*: More flexible for gradual rollout but heavyweight for MVP — a simple enum on Tenant and a filter attribute are sufficient and testable.

### 5. Demo seed data is a living, exhaustive showcase

**Decision:** `DemoSeedScript.SeedAsync` is treated as a first-class artifact alongside production code — it must exercise every capability the platform currently supports. It provisions **two marinas** with distinct characters so prospects see the platform working at different scales. Every entity category gets representative records: staff at multiple roles, customers with accounts and members, slips in every status, bookings across past/present/future, paid and outstanding invoices, and (as each phase ships) maintenance requests, announcements, work orders, and any future entity type. When a new phase adds a new entity, `DemoSeedScript` must be updated in the same PR — this is enforced by a CI integration test that calls `SeedAsync` and asserts at least one record exists for each known entity type. See CLAUDE.md for the standing rule.

**Alternatives considered:**
- *Minimal seed (one marina, a few records)*: Faster to write but gives prospects an unrealistically sparse view; misses the point of a capability showcase.
- *Separate seed scripts per phase*: Easier to maintain incrementally but creates gaps — a visitor could land in a demo that is missing the latest features.

### 6. Tier capability definitions live in a versioned registry, not in code

**Decision:** Introduce `TierCapabilityRegistry` — a static class in `MyMarina.Infrastructure` that returns the set of named capabilities included at each `SubscriptionTier`. Each capability is a string constant (e.g. `"announcements"`, `"multi-marina"`, `"advanced-reporting"`). The `[RequiresTier]` attribute is the enforcement mechanism; the registry is the source of truth for *what* each tier includes. The registry is a **living document** — as new features ship, the team decides which tier they belong to and updates the registry. The initial state is intentionally sparse: `Free` gets a very limited set (TBD in a pricing discussion), `Pro` and `Premium` get progressively more. The demo CTA on the marketing site can query `GET /api/demo/capabilities?tier=free` (which reads from the registry) so the pricing table always reflects the current truth without a frontend deploy.

**What is NOT decided here:** The specific feature-to-tier assignments for `Free`, `Pro`, and `Premium` are out of scope for this change. They will be defined in a follow-up pricing/feature-model discussion and recorded in the registry. The infrastructure is built now; the assignments come later.

**Alternatives considered:**
- *Hard-code tier checks inline at each endpoint*: Fastest to ship but scatters the capability map across the codebase; impossible to audit what each tier can do.
- *Database-driven feature flags*: Maximally flexible but adds admin UI, migrations, and cache invalidation complexity — overkill for a SaaS with three fixed tiers.
- *Per-tier policy classes (IAuthorizationPolicy)*: Clean ASP.NET Core pattern but verbose for simple tier comparisons; a filter attribute over an enum is sufficient and easier to reason about.

### 7. Marketing site deployment as a separate nginx target

**Decision:** The existing `nginx-ingress` routes `mymarina.org` (and `www.mymarina.org`) to the new marketing site static bundle, while `app.mymarina.org` routes to the SaaS SPA. The demo "Try Demo" button redirects to `app.mymarina.org` with the demo JWT. Both share the same `api.mymarina.org` backend.

## Risks / Trade-offs

- **Demo provisioning latency** → Creating a tenant + seeding data on each click adds ~1–2 seconds of API work. Mitigation: Run provisioning asynchronously where possible; show a loading state on the CTA button. Acceptable for MVP.
- **Demo endpoint abuse (tenant spam)** → An attacker could call `POST /api/demo/session` in a loop, filling the database. Mitigation: Rate-limit to 5 provisioning requests per IP per hour; the cleanup job reclaims storage every 15 minutes.
- **Demo schema drift** → If the backend schema changes, the seed script may break silently. Mitigation: Seed script is invoked as part of an integration test in CI; a broken seed fails the build before it ships.
- **Shared design tokens diverge** → Mitigation: Marketing site (`src/MyMarina.Marketing/`) imports Tailwind config from `src/MyMarina.Web/` via a relative path; token changes propagate automatically.
- **Astro unfamiliarity** → The team has no prior Astro experience. Mitigation: Astro's learning curve is shallow for a single-page marketing site; React islands handle any complex interactivity the team is already comfortable with.

## Open Questions

- **Domain strategy**: Is `mymarina.org` the confirmed root for the marketing site, or should it be a subdomain like `www.mymarina.org` while the SaaS lives at `app.mymarina.org`? This affects nginx routing config and SSL cert scope.
- **Demo personas**: How many demo roles should be pre-authenticated? Proposal covers operator + customer — is a platform-operator (admin) demo view also needed?
- **Contact / lead capture**: Should the marketing site include a contact form that sends an email, or just a mailto link for MVP?
