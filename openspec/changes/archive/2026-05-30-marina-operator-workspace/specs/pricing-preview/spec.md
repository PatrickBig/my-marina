## ADDED Requirements

### Requirement: PricingPreviewPanel renders inside workspace shell
The existing `PricingPreviewPanel` component SHALL render correctly inside the workspace shell's two-column Pricing screen layout. The component's props and behavior are unchanged; it is used in a new layout context without modification.

#### Scenario: Preview panel is visible on the Pricing screen
- **WHEN** an operator navigates to `/marina/:id/pricing` and selects a plan
- **THEN** the PricingPreviewPanel renders the rate examples in the right column of the workspace layout
