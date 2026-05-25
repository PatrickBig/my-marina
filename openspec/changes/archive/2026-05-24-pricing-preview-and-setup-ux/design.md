## Context

The pricing system introduced by the `pricing-plans` change uses three rate kinds — Flat, PerFoot, and PerArea — each of which produces a different resolved dollar amount depending on slip dimensions. When a marina operator configures a plan during setup or on the pricing plans page, they see inputs like "Amount: $0.18 / sq ft" with no immediate feedback on what that means for an actual slip. This requires mental arithmetic (0.18 × 40ft × 14ft = $100.80) that operators shouldn't have to do.

The onboarding form (`MarinaOnboardingPage`) also currently exposes the internal `Tenant` entity to operators as "Organization name." The Tenant abstraction is a multi-marina grouping concept — useful for enterprise customers running multiple marinas — but invisible and irrelevant for the typical single-marina operator. Every new signup creates exactly one tenant per marina, making the separation noise.

**Current state:**
- `PriceResolver` exists in C# (Application layer) but has no TypeScript equivalent.
- `MarinaOnboardingPage` requires two name fields; `CreateMarinaAccountCommand.TenantName` is non-optional.
- `Step5Pricing` and the `PlanForm` in `PricingPlansPage` show no feedback on computed rates.

## Goals / Non-Goals

**Goals:**
- Implement a pure TypeScript `resolvePrice(plan, slipLength, slipBeam, amenities)` function that mirrors `PriceResolver.Resolve` exactly.
- Embed a `PricingPreviewPanel` component in the pricing form in both the wizard (Step5Pricing) and `PricingPlansPage` that updates in real time as the operator changes rate inputs.
- Simplify `MarinaOnboardingPage` to a single "Marina name" field; derive `tenantName = marinaName` server-side.

**Non-Goals:**
- Persisting preview inputs anywhere (session storage, DB).
- A standalone preview page or API endpoint.
- Changing how the `PriceResolver` works on the backend — the TypeScript version mirrors it, not replaces it.
- Supporting multi-marina tenants in this change — that complexity is deferred.

## Decisions

### 1. Client-side resolver, not a new API endpoint

**Decision:** Implement the price calculation entirely in the browser.

**Rationale:** The resolver is a pure function with no database reads — it only needs `plan` (form state) + sample dimensions (local UI state). A round-trip API call would add latency, require a new endpoint, and create a dependency on the API being up during form interaction. The TypeScript port of `PriceResolver` is ~30 lines and trivially testable.

**Alternative considered:** `POST /marinas/{id}/pricing/plans/preview` with the current form state. Rejected — unnecessary coupling for a pure calculation.

### 2. Sample dimensions are user-controlled inputs in the preview panel, not fixed

**Decision:** Let the operator enter "preview slip" dimensions (length and beam) and toggle amenities in the preview panel. Default to 40ft × 14ft.

**Rationale:** A fixed sample size is misleading — a 20ft slip and a 60ft slip at the same per-foot rate produce very different prices. Giving operators control lets them immediately answer "what would my largest slip cost?" Defaulting to 40ft × 14ft covers the median recreational powerboat/sailboat, giving a useful starting point.

**Alternative considered:** Auto-derive from the marina's existing slip dimensions (average or max). Rejected — during initial setup, no slips may exist yet; also complicates the component interface.

### 3. `tenantName` derived from `marinaName` when absent

**Decision:** Make `TenantName` optional in `CreateMarinaAccountCommand` and `MarinaSignupRequest`. When absent, the handler defaults it to `MarinaName`.

**Rationale:** The API remains backwards-compatible (clients that pass `tenantName` still work; dockominium and private dock onboarding flows already pass a single name for both). The field is simply no longer required from the marina operator form.

**Alternative considered:** Remove `TenantName` from the API entirely. Rejected — the platform operator panel creates tenants explicitly, and future multi-marina enterprise signup will need it.

### 4. Preview panel as a co-located sub-component, not a modal

**Decision:** Render the preview panel inline, below the rate inputs, collapsible but visible by default.

**Rationale:** Operators need to see the effect of their rate changes immediately as they type — a modal requires an extra click and breaks the feedback loop. An inline collapsible panel keeps the context without dominating the form.

## Risks / Trade-offs

- **TypeScript resolver drift from C# resolver** → Mitigated by direct test parity: the resolver unit tests (currently in `PriceResolverTests.cs`) define the exact expected outputs; the TypeScript version must pass the same cases (documented in the spec).
- **PerArea rates with small dimensions look deceivingly cheap** → Accepted; the operator can adjust sample dimensions to any slip size they care about.
- **Tenant name no longer visible to marina operators post-signup** → Accepted; the platform operator panel retains full visibility and control over tenant names.

## Migration Plan

1. Deploy backend change (optional `tenantName` in request) — no migration needed; no schema change.
2. Deploy frontend changes independently — `MarinaOnboardingPage` simplification and pricing preview panels are purely additive/subtractive UI changes with no data dependency.
3. No rollback complexity — both changes are isolated to specific pages with no shared state.

## Open Questions

- Should the preview panel be shown on the `PricingPlansPage` edit form even when editing an existing plan (i.e., show the *current* resolved rate vs. the *pending* form state)? Current plan: always reflect live form state — operators can compare against what they know their slips currently earn.
