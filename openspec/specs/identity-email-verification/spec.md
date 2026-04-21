## ADDED Requirements

### Requirement: Email confirmation token generated on user creation
The system SHALL use `UserManager.GenerateEmailConfirmationTokenAsync` to create a confirmation token whenever a new `ApplicationUser` is created via an invite flow. The token SHALL be embedded in a confirmation link and delivered via `IEmailService`.

#### Scenario: Token is generated on customer invite
- **WHEN** `POST /customers/{id}/invite` creates a new `ApplicationUser`
- **THEN** a confirmation token is generated for that user
- **AND** a confirmation link is included in the invite email

#### Scenario: Token is generated on staff invite
- **WHEN** `POST /staff/invite` creates a new `ApplicationUser`
- **THEN** a confirmation token is generated for that user
- **AND** a confirmation link is included in the invite email

### Requirement: Email confirmation endpoint
The system SHALL expose `GET /auth/confirm-email?userId={userId}&token={token}` that validates the token and sets `ApplicationUser.EmailConfirmed = true`.

#### Scenario: Valid confirmation link
- **WHEN** a user clicks a valid confirmation link with correct `userId` and `token`
- **THEN** `UserManager.ConfirmEmailAsync` is called
- **AND** the endpoint returns HTTP 200 with a success message

#### Scenario: Invalid or expired token
- **WHEN** the confirmation request contains an invalid or expired token
- **THEN** the endpoint returns HTTP 400 with an error message

#### Scenario: Already confirmed
- **WHEN** the confirmation link is used for a user whose email is already confirmed
- **THEN** the endpoint returns HTTP 200 (idempotent — no error)

### Requirement: Confirmed email required for customer portal login
The system SHALL prevent portal login for customer accounts whose `ApplicationUser.EmailConfirmed` is `false` when `Email:RequireConfirmedEmail` is `true`.

#### Scenario: Unconfirmed customer is blocked from login
- **WHEN** a customer with `EmailConfirmed = false` attempts to authenticate
- **AND** `Email:RequireConfirmedEmail` is `true`
- **THEN** the auth endpoint returns HTTP 403 with error code `email_not_confirmed`

#### Scenario: Confirmed customer logs in normally
- **WHEN** a customer with `EmailConfirmed = true` authenticates
- **THEN** the auth flow proceeds normally and a JWT is issued

#### Scenario: Confirmation not required in development
- **WHEN** `Email:RequireConfirmedEmail` is `false`
- **THEN** customers with unconfirmed email addresses can log in without restriction

### Requirement: Confirmation link routes through the SPA
The confirmation URL embedded in invite emails SHALL point to the React SPA route `/confirm-email` with `userId` and `token` query parameters. The SPA route SHALL call `GET /auth/confirm-email` on the API and display a branded confirmation result page.

#### Scenario: Confirmation link format
- **WHEN** a confirmation email is generated
- **THEN** the link SHALL have the form `https://<domain>/confirm-email?userId=<id>&token=<encoded-token>`

#### Scenario: SPA confirmation page renders on valid response
- **WHEN** the user visits `/confirm-email?userId=...&token=...`
- **THEN** the SPA calls the API confirm endpoint
- **AND** displays a success or error message based on the response
