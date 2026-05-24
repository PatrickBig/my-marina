## MODIFIED Requirements

### Requirement: Marina onboarding form collects marina name and type only
The `MarinaOnboardingPage` form SHALL collect only **Marina name** and **Marina type** from the operator. The "Organization name" field SHALL be removed. The system SHALL derive the tenant name from the marina name automatically.

The form SHALL:
- Require **Marina name** (min 2 characters) — used for both `marinaName` and `tenantName` in the API call
- Require **Marina type** selection: Commercial, Yacht Club, or Private Community
- Submit to `POST /marinas/signup` passing `marinaName` as both `tenantName` and `marinaName`
- Remain functionally identical in all other respects (validation, error display, redirect to wizard on success)

#### Scenario: Successful signup with marina name only
- **WHEN** operator enters "Sunset Harbor Marina" and selects "Commercial" then submits
- **THEN** a tenant named "Sunset Harbor Marina" and a marina named "Sunset Harbor Marina" are created and the operator is redirected to the setup wizard

#### Scenario: Marina name validation still enforced
- **WHEN** operator submits with marina name shorter than 2 characters
- **THEN** an inline validation error is shown and the form is not submitted

#### Scenario: Organization name field is absent
- **WHEN** operator views the `MarinaOnboardingPage`
- **THEN** there is no "Organization name" input field on the form

---

## ADDED Requirements

### Requirement: API accepts tenantName as optional
The `POST /marinas/signup` endpoint SHALL accept `tenantName` as an optional field. When absent or null, the backend SHALL default `tenantName` to the value of `marinaName`.

#### Scenario: Signup without tenantName uses marinaName
- **WHEN** `POST /marinas/signup` is called with `marinaName: "Harbor View"` and no `tenantName`
- **THEN** the created tenant has `Name = "Harbor View"`

#### Scenario: Signup with explicit tenantName still works
- **WHEN** `POST /marinas/signup` is called with `marinaName: "Harbor Marina"` and `tenantName: "Harbor Corp LLC"`
- **THEN** the created tenant has `Name = "Harbor Corp LLC"` and the marina has `Name = "Harbor Marina"`
