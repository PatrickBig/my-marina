## 1. Client-Side Price Resolver

- [x] 1.1 Create `src/MyMarina.Web/src/utils/priceResolver.ts` exporting `resolvePrice(plan, slipLength, slipBeam, amenities): { transient: number | null; lease: number | null }` — mirrors backend `PriceResolver.Resolve` exactly (Flat / PerFoot / PerArea, add-on stacking, MinCharge floor)
- [x] 1.2 Write unit tests for `priceResolver.ts` covering: Flat rate, PerFoot rate, PerArea rate, null rate kind → null, amenity add-on stacking, MinCharge floor applied, MinCharge not applied when base exceeds it

## 2. PricingPreviewPanel Component

- [x] 2.1 Create `src/MyMarina.Web/src/components/PricingPreviewPanel.tsx` accepting props: current plan form state (rate kinds, amounts, min charges, amenity add-ons), defaulting sample length to 40 and beam to 14
- [x] 2.2 Render two number inputs for **Length (ft)** and **Beam (ft)** within the panel (local state, not persisted)
- [x] 2.3 Render amenity toggle checkboxes for each add-on that has a non-empty transient or lease amount configured in the current form state
- [x] 2.4 Display computed **Transient rate** and **Lease rate** using `resolvePrice`, formatted as currency with units (e.g., "$140.00 / night", "$3,500.00 / month"); show "—" when rate kind is null
- [x] 2.5 For `PerArea` rate kind, additionally show the breakdown formula (e.g., "$0.18 × 40ft × 14ft = $100.80") below the computed rate
- [x] 2.6 Recompute on every prop change and on sample dimension / amenity toggle changes (no debounce needed — pure sync calculation)

## 3. Integrate Preview Panel into Setup Wizard

- [x] 3.1 Add `PricingPreviewPanel` to `Step5Pricing` in `MarinaSetupWizardPage.tsx`, collapsed by default with a "Preview rates ▾" toggle button
- [x] 3.2 Pass current form state (transientRateKind, transientAmount, transientMinCharge, leaseRateKind, leaseAmount, leaseMinCharge, amenityRows) from wizard step state into the panel props on every render

## 4. Integrate Preview Panel into PricingPlansPage

- [x] 4.1 Add `PricingPreviewPanel` to the `PlanForm` component in `PricingPlansPage.tsx`, expanded by default, placed below the amenity add-on table
- [x] 4.2 Pass current form state from `PlanForm` into the panel props on every render

## 5. Simplified Marina Onboarding — Frontend

- [x] 5.1 Remove the "Organization name" field and `tenantName` validation from `MarinaOnboardingPage.tsx`
- [x] 5.2 Update the `onSubmit` handler to pass `marinaName` for both `tenantName` and `marinaName` in the `signupMarina` API call
- [x] 5.3 Update the Zod schema to remove the `tenantName` field

## 6. Simplified Marina Onboarding — Backend

- [x] 6.1 Make `TenantName` nullable (`string?`) in `MarinaSignupRequest` (the API request DTO)
- [x] 6.2 Make `TenantName` nullable in `CreateMarinaAccountCommand` record
- [x] 6.3 In `CreateMarinaAccountCommandHandler`, default `TenantName` to `MarinaName` when null or empty
- [x] 6.4 Verify `dotnet build` passes with no errors

## 7. Build Verification

- [x] 7.1 Run `npm run build` from `src/MyMarina.Web/` and fix any TypeScript errors
- [x] 7.2 Manually verify the setup wizard pricing step shows the preview panel (collapsed) and it expands and computes correctly
- [x] 7.3 Manually verify the pricing plans page editor shows the preview panel (expanded) and it updates in real time
- [x] 7.4 Manually verify the marina onboarding page has only one name field and signup still works end-to-end
