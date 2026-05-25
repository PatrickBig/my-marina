## Why

Two friction points in the marina setup experience hurt operator confidence during onboarding. First, abstract rate kinds (PerFoot, PerArea) are hard to reason about — operators can't tell what their pricing actually produces for a real slip until after the fact. Second, the "Create your marina" form asks for a separate "Organization name" alongside "Marina name," a distinction that means nothing to most operators and creates unnecessary confusion.

## What Changes

- **Remove "Organization name" field** from the marina onboarding form (`MarinaOnboardingPage`); derive the tenant name from the marina name automatically on the backend.
- **Add a live pricing preview panel** to the pricing form in both the setup wizard (`Step5Pricing`) and the pricing plans management page (`PricingPlansPage`). The panel lets the operator enter sample slip dimensions and instantly sees computed transient and lease rates — including amenity add-ons.
- Update the `CreateMarinaAccountCommand` to make `TenantName` optional/derived, defaulting it to `MarinaName` when not supplied.

## Capabilities

### New Capabilities

- `pricing-preview`: Live rate calculator panel embedded in pricing forms. Given user-entered sample slip dimensions (length, beam) and selected amenities, computes and displays the resolved transient and lease rates using the same `PriceResolver` logic already in the backend — implemented client-side in TypeScript.

### Modified Capabilities

- `marina-onboarding`: Remove the redundant "Organization name" field; simplify the form to marina name + marina type only.

## Impact

- **Frontend**: `MarinaOnboardingPage.tsx` (remove field), `MarinaSetupWizardPage.tsx` Step5Pricing (add preview panel), `PricingPlansPage.tsx` (add preview panel to create/edit form).
- **Backend**: `CreateMarinaAccountCommand` and `MarinaSignupRequest` — make `TenantName` optional; handler derives it from `MarinaName` when absent. No breaking change to the API contract (field stays optional for backwards compat).
- **No new API endpoints** — the preview calculator runs entirely client-side using the same rate logic already defined in the frontend.
