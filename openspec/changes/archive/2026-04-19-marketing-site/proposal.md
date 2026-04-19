## Why

MyMarina has no public-facing presence — there is nowhere for prospective marina operators or customers to learn about the platform, explore its capabilities, or try it before committing. A marketing site with an embedded interactive demo removes the friction between awareness and adoption, and gives the product a credible, shareable entrypoint before a paid account is needed.

## What Changes

- Add a new public marketing website (separate from the SaaS app) at the root domain (e.g. `mymarina.org`) that describes the product, its features, and target personas
- Add an interactive demo environment — a sandboxed, pre-seeded tenant that visitors can explore without creating an account
- The demo tenant is read-only or softly sandboxed (resets periodically), giving prospects a realistic feel for the operator and customer portal experiences
- No changes to the existing SaaS application routing or authentication flows
- Marketing site should contain screenshots, short recordings, etc of the product
- Marketing site MUST be kept up to date with new features. CRITICAL FOR FUTURE SPEC DEVELOPMENT

## Capabilities

### New Capabilities

- `marketing-landing-page`: Public marketing site — hero section, feature highlights, persona-based value props (marina operators, customers), pricing/contact CTA, and navigation to the demo
- `interactive-demo`: Sandboxed demo environment with pre-seeded marina data; visitors land in a guided, pre-authenticated operator or customer session to explore the product without signing up
- Should provide a fun, marketable, search engine optimized marketing site.
- Marketing is key. Capability should focus on providing a service that can be scraped by multiple search engines and be something that can receive traffic from other marketing sites like facebook

### Modified Capabilities

<!-- None — no existing spec behavior changes -->

## Impact

- **New site project**: Marketing site is a standalone frontend (likely same tech stack: React + Vite + Tailwind) deployed separately from the SaaS app, or as a distinct route on the root domain
- **Demo tenant infrastructure**: Requires a seeded demo tenant in the database (or a dedicated demo database), a demo reset mechanism (scheduled job or on-demand), and a bypass/auto-login flow for demo sessions
- **No API changes**: Demo environment reuses existing API endpoints — no new backend capabilities needed, only seed data and an auto-login shortcut
- **Deployment**: New deployment target (marketing site) alongside existing SaaS; demo environment may share the existing API/DB with an isolated tenant or use a separate stack
