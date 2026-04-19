## 1. Subscription Tier Model

- [x] 1.1 Add `SubscriptionTier` enum to `MyMarina.Domain`: `Free = 0`, `Pro = 1`, `Premium = 2`
- [x] 1.2 Add `SubscriptionTier` column (non-nullable, default `Free`) to the `Tenant` entity and EF configuration; create and apply migration
- [x] 1.3 Add a `[RequiresTier(SubscriptionTier.Pro)]` action filter attribute that reads `subscription_tier` from the JWT and returns 403 with a `tier_required` error code if the tenant's tier is below the required level
- [x] 1.4 Embed `subscription_tier` claim in JWT at login time via the existing `JwtTokenService`
- [x] 1.5 Add integration test: a `Free` tenant calling a `[RequiresTier(Pro)]` endpoint receives 403 with `tier_required`

## 2. Tier Capability Registry

- [x] 2.1 Create `TierCapabilityRegistry` static class in `MyMarina.Infrastructure/Demo/` with string constants for each named capability (e.g. `Capabilities.MultiMarina`, `Capabilities.Announcements`) — initial assignments are placeholder/TBD pending the pricing discussion; mark each unassigned constant with `// TBD: pricing`
- [x] 2.2 Add `GET /api/demo/capabilities?tier={tier}` endpoint that returns the capability list for the requested tier by reading from `TierCapabilityRegistry` — used by the marketing site pricing table so it always reflects the current state without a frontend deploy
- [x] 2.3 Seed `Free` tier with a deliberately minimal capability set (exact list TBD in pricing discussion — leave a `// TODO: pricing` block as a forcing function)
- [x] 2.4 Add a file-header comment to `TierCapabilityRegistry.cs`: *"When a new feature ships, add a capability constant here and assign it to one or more tiers in the same PR. See CLAUDE.md."*

## 3. Demo Tenant Lifecycle — Backend

- [x] 3.1 Add `IsDemo` (bool, default false) and `DemoExpiresAt` (nullable `DateTimeOffset`) columns to the `Tenant` entity and EF configuration; create and apply migration
- [x] 3.2 Add `Demo:TtlMinutes` (default: 60) and `Demo:DefaultTier` (default: `Pro`) to `appsettings.json`; bind to a `DemoOptions` settings class
- [x] 3.3 Create `DemoSeedScript` — a static class with `SeedAsync(AppDbContext db, Guid tenantId)` that provisions **two marinas** with distinct characters (e.g. a large commercial marina and a small community boatyard), each containing:
  - 2+ docks with named berths
  - 10–15 slips per marina (mix of available, occupied, reserved, under maintenance)
  - 3–5 marina staff `UserContext` records (Owner + Staff roles)
  - 4–6 customer accounts, each with an `ApplicationUser` + `UserContext` + `CustomerAccountMember` record
  - Active bookings, past bookings, and a future reservation per marina
  - Paid and outstanding invoices
  - At least one maintenance request per marina (once Phase 5 entities exist)
  - At least one announcement per marina (once Phase 5 entities exist)
  - *(This method is a living document — add new capability data here whenever a new phase adds entities. See CLAUDE.md.)*
- [x] 3.4 Create `ProvisionDemoTenantCommand` (accepts `role` + `tier` params) that: creates a new `Tenant` with `IsDemo=true`, `DemoExpiresAt=now+TTL`, and `SubscriptionTier=tier`; creates demo operator + customer `ApplicationUser` + `UserContext` records; runs `DemoSeedScript.SeedAsync`
- [x] 3.5 Create `DeleteExpiredDemoTenantsCommand` + handler that deletes all tenants where `IsDemo=true AND DemoExpiresAt < now()` — rely on EF cascade deletes for all child data
- [x] 3.6 Register `DeleteExpiredDemoTenantsCommand` as a Hangfire recurring job running every 15 minutes

## 4. Demo Session API Endpoint

- [x] 4.1 Add `DemoController` at `/api/demo` with `POST /session` accepting `role` (`operator` | `customer`) and `tier` (`free` | `pro` | `premium`) query params
- [x] 4.2 On request: call `ProvisionDemoTenantCommand`, then issue a signed JWT with `is_demo: true`, `subscription_tier`, and correct role/tenant claims scoped to the new tenant, TTL matching `Demo:TtlMinutes` — reuse existing `JwtTokenService`
- [x] 4.3 Return 400 for unsupported `role` or `tier` values; default `tier` to `Demo:DefaultTier` if omitted
- [x] 4.4 Apply ASP.NET Core rate limiting — 5 provisioning requests per IP per hour
- [x] 4.5 Add integration test: operator demo session provisions a new tenant with 2 marinas seeded and returns correct claims
- [x] 4.6 Add integration test: customer demo session scopes JWT to a customer context within the provisioned tenant
- [x] 4.7 Add integration test: a `free`-tier demo JWT is blocked by `[RequiresTier(Pro)]` endpoints
- [x] 4.8 Add integration test: expired demo tenants are deleted by the cleanup command and return 404 on subsequent requests

## 5. Demo UI Banner — SaaS Frontend

- [x] 5.1 Add `isDemoSession()` utility in [src/MyMarina.Web/src/lib/auth.ts](src/MyMarina.Web/src/lib/auth.ts) that reads `is_demo` from the decoded JWT in `sessionStorage`
- [x] 5.2 Store the demo JWT in `sessionStorage` (not `localStorage`) when the SaaS app receives it on redirect from the marketing site (via query param `?demo_token=...`)
- [x] 5.3 Create `DemoBanner` component displayed in the root layout when `isDemoSession()` is true — non-dismissible, shows session TTL countdown and current tier name, links back to `mymarina.org`
- [x] 5.4 Handle demo JWT expiry gracefully — on 401, detect `is_demo` was set and redirect to marketing site with `?expired=1` so a message can be shown

## 6. Marketing Site — Project Setup

- [x] 6.1 Create `src/MyMarina.Marketing/` as a new Astro project (`npm create astro@latest`) with TypeScript and Tailwind CSS integration (`@astrojs/tailwind`)
- [x] 6.2 Configure Tailwind to import and extend the token config from `src/MyMarina.Web/tailwind.config.ts` so both projects share colors, spacing, and font definitions
- [x] 6.3 Add `@astrojs/react` integration so React components can be used as islands for interactive elements (contact form, demo CTA)
- [x] 6.4 Add `@astrojs/sitemap` and configure `site` URL in `astro.config.mjs` for automatic sitemap generation
- [x] 6.5 Verify `src/MyMarina.Marketing/` is excluded from the `.sln` file and not picked up by `dotnet build`
- [x] 6.6 Add `dev:marketing` and `build:marketing` scripts to the root-level tooling (Makefile or root `package.json`)

## 7. Marketing Site — Landing Page Content

- [x] 7.1 Create `NavBar` Astro component with logo, anchor links to page sections (Features, Screenshots, Pricing, Demo, Contact), and a hamburger menu for viewports under 768px
- [x] 7.2 Create `HeroSection` with headline, subheadline, and a primary "Try Demo" CTA — React island that calls `POST /api/demo/session` with the default tier and redirects
- [x] 7.3 Create `PricingSection` — a React island that fetches `GET /api/demo/capabilities?tier=*` for each tier and renders a comparison table (Free / Pro / Premium); each tier card has a "Try this tier" CTA that passes the matching `tier` param to the demo session endpoint
- [x] 7.4 Create `FeaturesSection` with at least three feature cards: marina operator management, customer self-service portal, and platform overview
- [x] 7.5 Create `ScreenshotsSection` with captioned screenshots of the operator dashboard, slip map, customer portal, and invoicing screen — use optimized images via Astro's `<Image />` component
- [x] 7.6 Add a short product walkthrough video or animated GIF embed in or near the hero/screenshots section (placeholder asset acceptable for first iteration)
- [x] 7.7 Create `ContactSection` as a React island with name/email/message fields, Zod validation, and a `mailto:` fallback submit for MVP — show inline success confirmation on submit
- [x] 7.8 Create `Footer` with copyright year, links to Terms (placeholder), Privacy (placeholder), and the contact section anchor
- [x] 7.9 Assemble all sections into `src/pages/index.astro`

## 8. Marketing Site — SEO and Social

- [x] 8.1 Add a shared `<BaseHead>` Astro component that renders `<title>`, `<meta name="description">` (≤160 chars), canonical URL, and Open Graph tags (`og:title`, `og:description`, `og:image`, `og:type`)
- [x] 8.2 Create a 1200×630px Open Graph image (`public/og-image.png`) showing the MyMarina logo and tagline
- [x] 8.3 Add Twitter Card meta tags (`twitter:card`, `twitter:title`, `twitter:description`, `twitter:image`) to `BaseHead`
- [x] 8.4 Add `robots.txt` to `public/` allowing all crawlers and pointing to `/sitemap-index.xml`
- [x] 8.5 Verify the built site produces fully-rendered HTML with no client-side-only content (run `astro build` and inspect the output HTML)
- [x] 8.6 Confirm no horizontal overflow on 375px and 768px viewports (browser devtools check)
- [x] 8.7 Confirm all images have descriptive `alt` text; all interactive elements are keyboard-accessible and have visible focus styles

## 9. Deployment Configuration

- [x] 9.1 Add `src/MyMarina.Marketing/Dockerfile` (multi-stage: Node build with `astro build` → nginx serving `dist/`)
- [x] 9.2 Add `k8s/marketing/` with `Deployment`, `Service`, and `Ingress` manifests targeting `mymarina.org` and `www.mymarina.org`
- [x] 9.3 Update the existing SaaS app `Ingress` to use `app.mymarina.org` as its host rule
- [x] 9.4 Update `docker-compose.yml` to include the marketing site container (port 4321) for local dev
- [x] 9.5 Add a GitHub Actions workflow job to build and push the marketing site Docker image alongside the API image on push to `main`
