# platform-operator-password-reset Specification

## Purpose
TBD - created by archiving change platform-operator-user-mgmt. Update Purpose after archive.
## Requirements
### Requirement: Force password reset
Platform operators SHALL be able to invalidate a user's current password and trigger an automatic password reset email to be sent to the user. This allows operators to help users who have lost access to their accounts.

#### Scenario: Operator initiates password reset
- **WHEN** operator clicks "Send Password Reset" button on user profile
- **THEN** system invalidates current password, generates reset token, and sends reset email to user's email address

#### Scenario: Password reset email sent
- **WHEN** operator initiates forced password reset
- **THEN** user receives email with password reset link

#### Scenario: User resets password via link
- **WHEN** user clicks password reset link in email
- **THEN** user is directed to password reset form to set new password

### Requirement: Confirmation dialog
Before invalidating the user's password, the system SHALL display a confirmation dialog warning the operator that the user will need to reset their password via the email link.

#### Scenario: Confirm password reset
- **WHEN** operator initiates password reset
- **THEN** system shows confirmation dialog explaining that reset email will be sent

#### Scenario: Operator cancels password reset
- **WHEN** operator clicks "Cancel" in confirmation dialog
- **THEN** password remains unchanged and no email is sent

### Requirement: Audit logging for password reset
All password reset initiations SHALL be logged to the audit trail with operator name, target user, timestamp, and action type. The audit log SHALL NOT contain the password reset token or new password.

#### Scenario: Password reset recorded in audit log
- **WHEN** operator initiates password reset for user
- **THEN** AuditLog entry created with action "user.password_reset_requested", showing operator and target user (no token or sensitive data)

### Requirement: Authorization check
The password reset endpoint SHALL only be accessible to users with platform operator role.

#### Scenario: Non-operator attempts password reset
- **WHEN** non-operator user attempts to call password reset endpoint
- **THEN** system returns 403 Forbidden

### Requirement: Reset token expiration
The password reset token SHALL expire after a reasonable time period (e.g., 24 hours) to prevent token reuse.

#### Scenario: Expired reset token
- **WHEN** user tries to use an expired reset token
- **THEN** system displays error "Reset link has expired" and directs user to request new reset

### Requirement: User notification
After operator initiates password reset, the system SHALL send an email to the user's current email address with the reset link and instructions.

#### Scenario: Reset email contains instructions
- **WHEN** user receives password reset email
- **THEN** email includes clear instructions on how to set new password and warning that this was initiated by support staff

### Requirement: Session invalidation
When password reset is forced, all of the user's existing sessions (refresh tokens) SHALL be revoked so they cannot continue using old sessions.

#### Scenario: All sessions invalidated
- **WHEN** operator forces password reset
- **THEN** all user's active sessions are revoked and user must authenticate again with new password

