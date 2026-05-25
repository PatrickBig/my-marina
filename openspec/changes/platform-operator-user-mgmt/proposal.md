## Why

Platform operators currently have minimal user management tools — they can only view a basic user list and perform sign-out/deactivate actions. Support teams frequently encounter account access issues (lost emails, forgotten accounts, name mismatches) that require operator intervention. The lack of visibility into user assets (vessels, reservations, memberships) and modification capabilities limits their ability to troubleshoot and assist users effectively. This change enables operators to find users by multiple criteria, view complete account profiles, and modify account details — all with mandatory audit trails for compliance and troubleshooting.

## What Changes

- **Enhanced user search**: Find users by name (first/last), email, or phone number — not just substring matching
- **User profile drill-down**: View complete account details including vessels owned, reservations/bookings, memberships, and activity timeline
- **Email address modification**: Operators can update a user's email to resolve lost-access scenarios
- **Name modification**: Operators can correct first/last name if needed
- **Forced password reset**: Operators can invalidate a user's password and trigger an automatic password reset email
- **Comprehensive audit trail**: All operator actions logged with sanitized details (never passwords, card numbers, or account numbers)

## Capabilities

### New Capabilities
- `platform-operator-user-search`: Multi-field user search (name, email, phone) for platform operators
- `platform-operator-user-profile`: View complete user profile including personal details, vessels, reservations, memberships, and activity
- `platform-operator-user-email-change`: Allow operators to change user email address with validation and audit logging
- `platform-operator-user-name-change`: Allow operators to change user first/last name with audit logging
- `platform-operator-password-reset`: Allow operators to invalidate user password and send reset email automatically
- `platform-operator-audit-logging`: Comprehensive audit trail for all operator actions on user accounts

### Modified Capabilities
<!-- No existing capability requirements are changing in this phase -->

## Impact

- **Backend**: Extend `PlatformOperatorController` with new endpoints, add command handlers for user modifications, enhance user queries
- **Database**: No schema changes required (ApplicationUser already has required fields; AuditLog infrastructure exists)
- **Frontend**: Create/enhance platform admin user management screens, add profile drill-down view, add action buttons
- **API Contract**: New GET/PATCH endpoints, regenerate OpenAPI types via `npm run generate-api`
- **Authorization**: All endpoints require `IsPlatformOperator = true`
- **Audit Trail**: Leverage existing `AuditLog` entity with standardized action naming convention
