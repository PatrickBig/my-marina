## MODIFIED Requirements

### Requirement: POST /customers/{id}/invite endpoint creates user for selected customer
The system SHALL accept a POST request to invite a customer by CustomerAccountId. The endpoint creates an ApplicationUser with UserRole.Customer and a CustomerAccountMember (Owner role), returning a generated temporary password. It SHALL also generate an email confirmation token and trigger an invite email via `IEmailService`.

#### Scenario: Valid invite generates user and sends email
- **WHEN** operator POSTs to /customers/{customerAccountId}/invite
- **THEN** the system verifies the CustomerAccount exists and belongs to the operator's marina
- **AND** a new ApplicationUser is created with a temporary password
- **AND** a CustomerAccountMember record is created linking the user to the account (Owner role)
- **AND** an email confirmation token is generated for the new user
- **AND** `IEmailService.SendCustomerInviteAsync` is called with the customer's email, name, temporary password, and confirmation link
- **AND** HTTP 201 response includes the temporary password

#### Scenario: Customer already has user
- **WHEN** operator tries to invite a CustomerAccount that already has an ApplicationUser
- **THEN** the system returns HTTP 409 Conflict
- **AND** the response indicates the customer already has a login

#### Scenario: Customer not found
- **WHEN** operator POSTs with a non-existent CustomerAccountId
- **THEN** the system returns HTTP 404 Not Found

#### Scenario: Customer belongs to different marina
- **WHEN** operator attempts to invite a customer from a different marina
- **THEN** the system returns HTTP 403 Forbidden
- **AND** prevents cross-marina customer invite

#### Scenario: Email send failure does not cancel invite
- **WHEN** `IEmailService.SendCustomerInviteAsync` throws during the invite
- **THEN** the exception is caught and logged as a warning
- **AND** the endpoint still returns HTTP 201 with the temporary password
