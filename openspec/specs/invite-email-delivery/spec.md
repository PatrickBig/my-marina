## ADDED Requirements

### Requirement: Customer invite email is sent on successful invite
The system SHALL call `IEmailService.SendCustomerInviteAsync` after successfully creating a customer `ApplicationUser`. The email SHALL be sent to the `CustomerAccount.Email` address.

#### Scenario: Invite email delivered on success
- **WHEN** `POST /customers/{id}/invite` creates a user successfully
- **THEN** `IEmailService.SendCustomerInviteAsync` is called with the customer's email address, their name, the temporary password, and the email confirmation link
- **AND** the API response still returns HTTP 201 with the temporary password

#### Scenario: Email send failure does not fail the invite
- **WHEN** `IEmailService.SendCustomerInviteAsync` throws an exception
- **THEN** the exception is caught and logged as a warning
- **AND** the invite still returns HTTP 201 — email delivery failure is non-fatal

### Requirement: Staff invite email is sent on successful invite
The system SHALL call `IEmailService.SendStaffInviteAsync` after successfully creating a staff `ApplicationUser`. The email SHALL be sent to the provided staff email address.

#### Scenario: Staff invite email delivered on success
- **WHEN** `POST /staff/invite` creates a user successfully
- **THEN** `IEmailService.SendStaffInviteAsync` is called with the staff member's email, name, role, marina name, temporary password, and confirmation link

#### Scenario: Staff email send failure is non-fatal
- **WHEN** `IEmailService.SendStaffInviteAsync` throws an exception
- **THEN** the exception is caught and logged as a warning
- **AND** the staff invite still returns HTTP 201

### Requirement: Customer invite email content
The customer invite email SHALL include: the marina name, the recipient's first name, a temporary password, a prominent confirmation link button, and instructions to log in at the portal URL.

#### Scenario: Email contains required fields
- **WHEN** a customer invite email is rendered
- **THEN** it SHALL contain the recipient's name, the marina's name, the temporary password, and a clickable confirmation link

### Requirement: Staff invite email content
The staff invite email SHALL include: the marina name, the recipient's name, their assigned role, a temporary password, a prominent confirmation link button, and instructions to log in at the operator URL.

#### Scenario: Staff email contains required fields
- **WHEN** a staff invite email is rendered
- **THEN** it SHALL contain the staff member's name, the marina name, their role, the temporary password, and a clickable confirmation link

### Requirement: Integration tests use NullEmailService
All integration tests that exercise invite or confirmation endpoints SHALL have `NullEmailService` registered so no real emails are sent during CI.

#### Scenario: No real emails sent during tests
- **WHEN** integration tests call invite endpoints
- **THEN** `NullEmailService` intercepts the send call
- **AND** no SMTP connection is attempted
