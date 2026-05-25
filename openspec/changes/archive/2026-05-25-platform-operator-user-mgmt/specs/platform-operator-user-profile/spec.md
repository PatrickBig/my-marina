## ADDED Requirements

### Requirement: View complete user profile
Platform operators SHALL be able to view a comprehensive user profile showing personal account details, all vessels (boats) owned by the user, all reservations, memberships, and account activity.

#### Scenario: Open user profile
- **WHEN** operator clicks on a user from search results
- **THEN** system displays complete user profile page with all account information

### Requirement: Personal account details display
The profile SHALL display user's name, email, phone number, account status (active/deactivated), email confirmation state, account creation date, and last login timestamp.

#### Scenario: View personal details
- **WHEN** operator views user profile
- **THEN** profile shows first name, last name, email, status, email confirmed flag, created date, and last login date

### Requirement: Vessels (boats) listing
The profile SHALL display all vessels owned by the user, including vessel name, boat type, dimensions (length, beam, draft), make/model, year, registration number, and archived status.

#### Scenario: View owned vessels
- **WHEN** operator views user profile with owned vessels
- **THEN** profile displays list of all vessels with name, type, dimensions, and status

#### Scenario: User with no vessels
- **WHEN** operator views profile of user with no vessels
- **THEN** profile shows "No vessels" message

#### Scenario: Show archived vessels
- **WHEN** viewing vessels
- **THEN** archived vessels are marked as archived but still visible (not hidden)

### Requirement: Reservations (bookings) listing
The profile SHALL display user's reservations grouped by status (upcoming, past, cancelled), showing slip location, dates (arrive/depart), marina, status, and pricing summary.

#### Scenario: View upcoming reservations
- **WHEN** operator views user profile
- **THEN** upcoming reservations show slip, marina, dates, and status

#### Scenario: View past reservations
- **WHEN** operator expands "Past Reservations" section
- **THEN** system displays completed reservations with historical pricing information

#### Scenario: View cancelled reservations
- **WHEN** operator views cancelled reservations section
- **THEN** shows reason for cancellation and cancellation date (if available)

### Requirement: Memberships display
The profile SHALL display all memberships the user holds (user → marina or tenant with assigned role).

#### Scenario: View user memberships
- **WHEN** operator views user profile
- **THEN** profile shows all memberships with marina/tenant name, role, and membership date

#### Scenario: User with no memberships
- **WHEN** user has no memberships
- **THEN** profile shows "No memberships" message

### Requirement: Activity timeline
The profile SHALL display an activity timeline showing recent account actions (logins, reservations created, vessels added, etc.) ordered by date descending.

#### Scenario: View activity timeline
- **WHEN** operator views user profile
- **THEN** activity section shows chronological list of account events

### Requirement: Authorization check
The user profile endpoint SHALL only be accessible to users with platform operator role.

#### Scenario: Non-operator accesses profile
- **WHEN** non-operator user attempts to view user profile endpoint
- **THEN** system returns 403 Forbidden

### Requirement: Data redaction for sensitive information
The profile SHALL NOT display payment method details, card numbers, billing account numbers, or password information. Pricing information (reservation costs) MAY be displayed.

#### Scenario: Financial details not shown
- **WHEN** viewing user profile and reservations
- **THEN** reservation pricing is shown but card/account numbers are never displayed
