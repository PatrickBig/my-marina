# platform-operator-user-email-change Specification

## Purpose
TBD - created by archiving change platform-operator-user-mgmt. Update Purpose after archive.
## Requirements
### Requirement: Change user email address
Platform operators SHALL be able to change a user's email address to resolve lost-access scenarios. The system SHALL validate that the new email is not already in use and follows standard email format.

#### Scenario: Operator changes user email
- **WHEN** operator enters new email address and confirms the change
- **THEN** system updates user email and marks it as confirmed

#### Scenario: Email already in use
- **WHEN** operator tries to change email to one that already exists in the system
- **THEN** system displays error "Email already in use" and prevents the change

#### Scenario: Invalid email format
- **WHEN** operator tries to change email to invalid format (e.g., "notanemail")
- **THEN** system displays error "Invalid email format" and prevents the change

### Requirement: Email confirmation behavior
When an operator changes a user's email, the new email SHALL be automatically marked as confirmed (operator is acting as trusted authority).

#### Scenario: Email confirmed after operator change
- **WHEN** operator changes email address
- **THEN** system marks new email as confirmed (no confirmation email required from user)

### Requirement: Audit logging for email changes
All email changes SHALL be logged to the audit trail with the operator name, old email, new email, timestamp, and action type.

#### Scenario: Email change recorded in audit log
- **WHEN** operator changes email from old@example.com to new@example.com
- **THEN** AuditLog entry created with action "user.email_changed", showing old and new email addresses (sanitized, never logging password or sensitive data)

### Requirement: Confirmation dialog
Before applying the email change, the system SHALL display a confirmation dialog showing the old and new email addresses.

#### Scenario: Confirm email change
- **WHEN** operator submits email change form
- **THEN** system shows confirmation dialog with "Are you sure?" message before proceeding

### Requirement: Authorization check
The email change endpoint SHALL only be accessible to users with platform operator role.

#### Scenario: Non-operator attempts email change
- **WHEN** non-operator user attempts to call email change endpoint
- **THEN** system returns 403 Forbidden

### Requirement: User notification (optional)
After email change, the system MAY send a notification email to the user's new email address. Any such notification SHALL NOT include sensitive account details beyond confirming the change was made by support staff.

#### Scenario: User receives email change notification
- **WHEN** operator changes user email
- **THEN** system optionally sends notification to new email address (implementation detail - not mandatory for phase 1)

### Requirement: Case-insensitive email handling
Email addresses SHALL be treated as case-insensitive for uniqueness checks (e.g., "John@Example.com" and "john@example.com" are the same).

#### Scenario: Duplicate check ignores case
- **WHEN** operator tries to change email to variation of existing email (different case)
- **THEN** system prevents change with "Email already in use" error

