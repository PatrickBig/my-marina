## ADDED Requirements

### Requirement: OperatorButton in NavBar based on marina membership count
The NavBar SHALL render an operator entry button whose behavior depends on the user's marina membership count:
- **0 marina memberships** — button is not rendered
- **1 marina membership** — an anchor-icon button that navigates directly to `/marina/:id/dashboard`
- **2+ marina memberships** — an anchor-icon button that opens a Radix `DropdownMenu` listing each marina (name + tier badge), each item navigating to that marina's `/dashboard`, plus a "View all →" item navigating to `/my-marinas`

The button SHALL be placed in the NavBar's right section, before the user profile area.

#### Scenario: User with no marina access sees no button
- **WHEN** the authenticated user has zero marina memberships
- **THEN** no operator button appears in the NavBar

#### Scenario: Single-marina user navigates directly
- **WHEN** the authenticated user has exactly one marina membership and clicks the operator button
- **THEN** the user is navigated to `/marina/:id/dashboard` without an intermediate picker

#### Scenario: Multi-marina user sees a dropdown
- **WHEN** the authenticated user has two or more marina memberships and clicks the operator button
- **THEN** a dropdown appears listing all their marinas by name with tier badge and a "View all →" option

### Requirement: /my-marinas listing page
The `/my-marinas` route SHALL render a page listing all marinas for which the authenticated user holds an operator membership. Each marina is shown as a card with: name, marina type, address (city + state), and tier badge. Each card has an "Open" button navigating to `/marina/:id/dashboard`. A "+ New marina" action links to `/marina/new`.

If the user has exactly one marina and navigates to `/my-marinas` directly (e.g., via "View all →"), the page SHALL still render the list (not auto-redirect), giving the user a consistent fallback.

#### Scenario: All operator marinas are listed
- **WHEN** an authenticated user with 3 marina memberships navigates to `/my-marinas`
- **THEN** 3 marina cards are rendered

#### Scenario: "Open" navigates to workspace
- **WHEN** a user clicks "Open" on a marina card
- **THEN** the user is navigated to `/marina/:id/dashboard` for that marina

#### Scenario: Unauthenticated access is rejected
- **WHEN** an unauthenticated user navigates to `/my-marinas`
- **THEN** the user is redirected to `/login`
