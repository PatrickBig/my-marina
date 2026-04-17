## ADDED Requirements

### Requirement: POST /customers/{id}/invite endpoint creates user for selected customer
The system SHALL accept a POST request to invite a customer by CustomerAccountId. The endpoint creates an ApplicationUser with UserRole.Customer and a CustomerAccountMember (Owner role), returning a generated temporary password.

#### Scenario: Valid invite generates user and password
- **WHEN** operator POSTs to /customers/{customerAccountId}/invite
- **THEN** the system verifies the CustomerAccount exists and belongs to the operator's marina
- **AND** a new ApplicationUser is created with a temporary password
- **AND** a CustomerAccountMember record is created linking the user to the account (Owner role)
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

### Requirement: No email or name input required on invite
The invite endpoint SHALL NOT require email or name in the request body, as these are already stored in the CustomerAccount record.

#### Scenario: Request body is empty or minimal
- **WHEN** operator POSTs to /customers/{id}/invite
- **THEN** no email or name fields are expected in the request body
- **AND** these values are retrieved from the existing CustomerAccount record
