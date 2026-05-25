## ADDED Requirements

### Requirement: Comprehensive audit trail
All platform operator actions on user accounts SHALL be logged to the AuditLog with complete details including who performed the action, what action was taken, which user was affected, timestamp, and sanitized details.

#### Scenario: Action is recorded in audit log
- **WHEN** operator performs any account modification action
- **THEN** AuditLog entry is created with all relevant information

### Requirement: Audit log schema
Each audit log entry SHALL include:
- ActorUserId: ID of the operator performing the action
- TargetType: "User" for user account actions
- TargetId: ID of the user being modified
- Action: Standardized action name (e.g., "user.email_changed", "user.password_reset_requested")
- Details: Human-readable description of what changed (e.g., "Changed email from old@example.com to new@example.com")
- OccurredAt: UTC timestamp of the action
- TenantId: null (platform-level operations, not tenant-specific)

#### Scenario: Audit entry contains required fields
- **WHEN** operator changes user email
- **THEN** AuditLog entry contains operator ID, user ID, action name, details, and timestamp

### Requirement: Sanitized details
Audit log details SHALL NEVER contain passwords, password reset tokens, card numbers, account numbers, or other sensitive personally identifiable information (PII). Details SHALL describe what changed in human-readable form.

#### Scenario: Password change not logged
- **WHEN** operator initiates password reset
- **THEN** audit log shows "Operator Pat initiated password reset for user John Doe" but never includes the password or token

#### Scenario: Email change shows both addresses
- **WHEN** operator changes email
- **THEN** audit log shows "Operator Pat changed email for user John Doe from old@example.com to new@example.com"

#### Scenario: Card numbers never logged
- **WHEN** any billing operation occurs (future phases)
- **THEN** audit log never contains credit card numbers or account numbers (example of constraint for future code)

### Requirement: Standard action naming convention
All operator actions SHALL use a standard naming format: "user.<action_type>" (e.g., "user.email_changed", "user.name_changed", "user.password_reset_requested", "user.force_signout", "user.deactivated", "user.activated").

#### Scenario: Consistent action names
- **WHEN** different operators perform same action
- **THEN** audit log entries use identical action name string

### Requirement: Audit log search and filtering
Platform operators SHALL be able to view the audit log and filter by user, date range, action type, and operator.

#### Scenario: Filter audit log by user
- **WHEN** operator views audit log and filters by specific user
- **THEN** system displays all actions performed on that user

#### Scenario: Filter audit log by date range
- **WHEN** operator specifies start and end date
- **THEN** system displays actions within that timeframe

#### Scenario: Filter audit log by action type
- **WHEN** operator filters by action type "user.email_changed"
- **THEN** system displays only email change actions

### Requirement: Audit log immutability
Audit log entries SHALL be immutable. Once created, entries cannot be edited, deleted, or modified.

#### Scenario: Cannot edit audit entry
- **WHEN** operator views audit log
- **THEN** no edit or delete options are available for audit entries

### Requirement: Pagination for audit log
Audit log results SHALL be paginated with configurable page size (default 50) to handle large result sets efficiently.

#### Scenario: View paginated audit log
- **WHEN** operator views audit log
- **THEN** results are shown in pages of 50 entries per page (default)

### Requirement: Authorization for audit log access
Only platform operators (IsPlatformOperator = true) SHALL be able to access the audit log.

#### Scenario: Non-operator cannot view audit log
- **WHEN** non-operator user attempts to access audit log endpoint
- **THEN** system returns 403 Forbidden

### Requirement: Operator name in audit log
Audit log entries SHALL display the operator's name (first and last) in human-readable format for easy reading, not just operator ID.

#### Scenario: Audit log shows operator name
- **WHEN** viewing audit log entry
- **THEN** entry displays "Operator Pat Bigler" not just the user ID

### Requirement: Action-specific audit logging
Each action type SHALL include details specific to that action in the audit log entry. For example:
- Email changes show old and new email
- Name changes show which field (first/last) and old/new values
- Password reset shows only that reset was initiated
- Sign-out/deactivate MAY include reason if provided

#### Scenario: Email change includes both addresses
- **WHEN** operator changes email
- **THEN** audit log details include "from: old@example.com to: new@example.com"

#### Scenario: Deactivation includes reason
- **WHEN** operator deactivates user with reason
- **THEN** audit log details include the provided reason
