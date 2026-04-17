## ADDED Requirements

### Requirement: Marina operator selects existing customer during invite
The system SHALL allow marina operators to select an existing CustomerAccount from a list rather than manually entering email and name. The selected account is then invited to create a login.

#### Scenario: Operator views invite modal with account list
- **WHEN** operator clicks "Add Customer" from the customer list
- **THEN** a modal appears showing all CustomerAccount records for the marina (name, email, status)
- **AND** accounts already linked to users are marked as unavailable or hidden

#### Scenario: Operator selects and invites existing account
- **WHEN** operator selects an account from the list and confirms
- **THEN** the system invites that CustomerAccountId (no email/name re-entry required)
- **AND** a temporary password is generated and displayed

#### Scenario: Selected account already has a user
- **WHEN** operator tries to invite an account that already has an associated user
- **THEN** the system returns HTTP 409 Conflict with message "This account already has a login"
- **AND** the modal remains open for account re-selection
