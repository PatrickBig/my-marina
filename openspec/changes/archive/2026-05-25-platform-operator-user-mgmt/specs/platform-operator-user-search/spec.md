## ADDED Requirements

### Requirement: Multi-field user search
Platform operators SHALL be able to search for users by name (first or last), email, or phone number. The search interface SHALL support partial matching and be case-insensitive.

#### Scenario: Search by email
- **WHEN** operator searches for "john@example.com"
- **THEN** system returns users matching that email address

#### Scenario: Search by first name
- **WHEN** operator searches for "John"
- **THEN** system returns all users with first name containing "John"

#### Scenario: Search by last name
- **WHEN** operator searches for "Smith"
- **THEN** system returns all users with last name containing "Smith"

#### Scenario: Search by phone number
- **WHEN** operator searches for "555-1234"
- **THEN** system returns users with phone number containing that pattern

#### Scenario: Case-insensitive matching
- **WHEN** operator searches for "JOHN"
- **THEN** system returns users regardless of case (matches "john", "John", "JOHN")

### Requirement: Search results display
Search results SHALL display user summary information including name, email, status (active/deactivated), email confirmation state, and last login timestamp.

#### Scenario: Search returns user summary
- **WHEN** operator searches and results are found
- **THEN** each result shows name, email, status, email confirmed flag, and last login date

#### Scenario: Empty search results
- **WHEN** operator searches with no matches
- **THEN** system displays "No users found" message

### Requirement: Search result pagination
Results SHALL be paginated with configurable page size (default 25) and support navigation through pages.

#### Scenario: Navigate to next page
- **WHEN** operator is viewing page 1 with results
- **THEN** operator can click "Next" to view page 2 of results

#### Scenario: Adjust page size
- **WHEN** operator changes page size to 50
- **THEN** system refetches results with new page size and resets to page 1

### Requirement: Authorization check
The search endpoint SHALL only be accessible to users with platform operator role (IsPlatformOperator = true).

#### Scenario: Non-operator accesses search
- **WHEN** non-operator user attempts to call search endpoint
- **THEN** system returns 403 Forbidden
