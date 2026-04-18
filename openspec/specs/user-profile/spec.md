### Requirement: View profile
The system SHALL provide an endpoint that returns the authenticated user's current profile information including display name, email address, and phone number.

#### Scenario: Fetch own profile
- **WHEN** an authenticated user sends `GET /profile`
- **THEN** the system returns 200 with the user's display name, email, and phone number

#### Scenario: Unauthenticated request
- **WHEN** a request is made to `GET /profile` without a valid JWT
- **THEN** the system returns 401 Unauthorized

---

### Requirement: Update profile
The system SHALL allow an authenticated user to update their display name and phone number via `PUT /profile`.

#### Scenario: Successful profile update
- **WHEN** an authenticated user sends `PUT /profile` with a valid display name and/or phone number
- **THEN** the system updates the values and returns 200 with the updated profile

#### Scenario: Invalid display name (empty)
- **WHEN** an authenticated user sends `PUT /profile` with an empty display name
- **THEN** the system returns 400 Bad Request with a validation error

---

### Requirement: Change email
The system SHALL allow an authenticated user to change their email address by providing their current password for confirmation.

#### Scenario: Successful email change
- **WHEN** an authenticated user sends `POST /profile/change-email` with a new valid email and correct current password
- **THEN** the system updates the email and returns 200

#### Scenario: Email already in use
- **WHEN** an authenticated user sends `POST /profile/change-email` with an email address already registered to another user
- **THEN** the system returns 409 Conflict

#### Scenario: Incorrect current password
- **WHEN** an authenticated user sends `POST /profile/change-email` with an incorrect current password
- **THEN** the system returns 400 Bad Request

#### Scenario: Invalid email format
- **WHEN** an authenticated user sends `POST /profile/change-email` with a malformed email address
- **THEN** the system returns 400 Bad Request with a validation error

---

### Requirement: Change password
The system SHALL allow an authenticated user to change their password by providing their current password and a new password meeting the configured policy.

#### Scenario: Successful password change
- **WHEN** an authenticated user sends `POST /profile/change-password` with the correct current password and a valid new password
- **THEN** the system updates the password and returns 200

#### Scenario: Incorrect current password
- **WHEN** an authenticated user sends `POST /profile/change-password` with an incorrect current password
- **THEN** the system returns 400 Bad Request

#### Scenario: New password does not meet policy
- **WHEN** an authenticated user sends `POST /profile/change-password` with a new password that fails the configured password rules
- **THEN** the system returns 400 Bad Request with descriptive validation errors

#### Scenario: New password same as current
- **WHEN** an authenticated user sends `POST /profile/change-password` where new password equals current password
- **THEN** the system returns 400 Bad Request

---

### Requirement: Profile page in SPA
The system SHALL provide a `/profile` page in the React SPA accessible to all authenticated users from both the operator and customer portal layouts.

#### Scenario: Navigate to profile from operator layout
- **WHEN** an authenticated operator user clicks the profile link in the navigation
- **THEN** the browser navigates to `/profile` and displays the profile form

#### Scenario: Navigate to profile from portal layout
- **WHEN** an authenticated customer user clicks the profile link in the navigation
- **THEN** the browser navigates to `/profile` and displays the profile form

#### Scenario: Profile form pre-filled
- **WHEN** the profile page loads
- **THEN** the display name, email, and phone fields are pre-populated with the current user's data

#### Scenario: Unauthenticated access redirected
- **WHEN** an unauthenticated user navigates directly to `/profile`
- **THEN** the router redirects them to the login page
