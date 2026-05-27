## ADDED Requirements

### Requirement: Pricing screen is absorbed into workspace shell
The existing `PricingPlansPage.tsx` SHALL have its standalone `<NavBar />` and page-level layout wrapper removed. The screen SHALL use `<PageHeader>` + `<PageBody>` and render inside `MarinaWorkspaceLayout`. The route SHALL be `/marina/:marinaId/pricing` (unchanged URL). All existing plan CRUD functionality SHALL be preserved. Visual spec: `docs/design_handoff_mymarina_marina_operator/screens-marina-setup.md#pricing-plans`.

#### Scenario: Pricing page shows workspace sidebar
- **WHEN** an operator navigates to `/marina/:id/pricing`
- **THEN** the workspace sidebar is visible alongside the pricing plan content (no second NavBar)

### Requirement: Plan selection and preview via URL params
The selected plan id SHALL be tracked in `?id` (default: first plan). Edit/new mode SHALL be tracked in `?mode` (`view | edit | new`, default: `view`). Bulk-assign dialog SHALL be triggered by `?bulk=<planId>`.

#### Scenario: Selecting a plan updates ?id
- **WHEN** an operator clicks a pricing plan card
- **THEN** `?id=<planId>` is in the URL and the preview sidebar shows that plan's details

### Requirement: Slips count link deep-links to Slips screen
Each plan card's "N slips" count SHALL be a link navigating to `/marina/:id/slips?plan=<planId>`.

#### Scenario: Clicking slips count navigates to filtered slips screen
- **WHEN** an operator clicks "18 slips" on a plan card
- **THEN** navigation goes to `/marina/:id/slips?plan=<planId>`
