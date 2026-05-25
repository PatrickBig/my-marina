## ADDED Requirements

### Requirement: Change user first name
Platform operators SHALL be able to change a user's first name to correct profile information or address name changes requested by users.

#### Scenario: Operator changes user first name
- **WHEN** operator enters new first name and confirms the change
- **THEN** system updates user's first name

#### Scenario: Empty first name rejected
- **WHEN** operator tries to set first name to empty string
- **THEN** system displays error "First name cannot be empty" and prevents the change

### Requirement: Change user last name
Platform operators SHALL be able to change a user's last name for the same reasons as first name.

#### Scenario: Operator changes user last name
- **WHEN** operator enters new last name and confirms the change
- **THEN** system updates user's last name

#### Scenario: Empty last name rejected
- **WHEN** operator tries to set last name to empty string
- **THEN** system displays error "Last name cannot be empty" and prevents the change

### Requirement: Audit logging for name changes
All name changes SHALL be logged to the audit trail with the operator name, old name, new name, timestamp, and action type. The log entry SHALL identify which field was changed (first or last name).

#### Scenario: Name change recorded in audit log
- **WHEN** operator changes first name from "John" to "Jon"
- **THEN** AuditLog entry created with action "user.first_name_changed", showing old and new values

#### Scenario: Last name change recorded in audit log
- **WHEN** operator changes last name from "Smith" to "Jones"
- **THEN** AuditLog entry created with action "user.last_name_changed", showing old and new values

### Requirement: Confirmation dialog
Before applying name changes, the system SHALL display a confirmation dialog showing the old and new name.

#### Scenario: Confirm name change
- **WHEN** operator submits name change form
- **WHEN** operator confirms in the dialog
- **THEN** system applies the change and displays success message

### Requirement: Authorization check
The name change endpoint SHALL only be accessible to users with platform operator role.

#### Scenario: Non-operator attempts name change
- **WHEN** non-operator user attempts to call name change endpoint
- **THEN** system returns 403 Forbidden

### Requirement: Batch name changes
Operators SHALL be able to change both first and last name in a single request if needed.

#### Scenario: Change both names at once
- **WHEN** operator changes both first and last name in one form
- **THEN** system updates both fields and logs both changes as separate audit entries
