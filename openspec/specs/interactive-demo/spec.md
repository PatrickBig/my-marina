## ADDED Requirements

### Requirement: Demo session creation endpoint
The API SHALL expose `POST /api/demo/session` accepting a `role` query parameter (`operator` or `customer`) that returns a short-lived demo JWT without requiring credentials.

#### Scenario: Visitor requests an operator demo session
- **WHEN** `POST /api/demo/session?role=operator` is called
- **THEN** a 200 response is returned with a signed JWT containing `is_demo: true`, `role: operator`, scoped to the demo tenant, with a 30-minute TTL

#### Scenario: Visitor requests a customer demo session
- **WHEN** `POST /api/demo/session?role=customer` is called
- **THEN** a 200 response is returned with a signed JWT containing `is_demo: true`, `role: customer`, scoped to the demo tenant customer account, with a 30-minute TTL

#### Scenario: Invalid role returns 400
- **WHEN** `POST /api/demo/session?role=admin` is called with an unsupported role value
- **THEN** a 400 Bad Request response is returned with a validation error message

#### Scenario: Endpoint is rate-limited
- **WHEN** more than 20 requests per minute are received from the same IP address
- **THEN** subsequent requests within that window return 429 Too Many Requests

### Requirement: Demo session is stored in session storage
The SaaS frontend SHALL store the demo JWT in `sessionStorage` (not `localStorage`) so the session is scoped to the browser tab and does not persist after the tab is closed.

#### Scenario: Demo JWT is stored in sessionStorage after redirect
- **WHEN** the visitor clicks "Try Demo" on the marketing site and is redirected to the SaaS app with a demo JWT
- **THEN** the JWT is stored in `sessionStorage` and the visitor is authenticated as the demo user

#### Scenario: Demo session does not persist after tab close
- **WHEN** the visitor closes the browser tab
- **THEN** the demo JWT is no longer present and the visitor must click "Try Demo" again to get a new session

### Requirement: Demo write guard
The API SHALL block writes that would modify demo tenant structure or user accounts when the request carries an `is_demo: true` JWT claim, while allowing writes to operational data (bookings, payments, maintenance requests, announcements).

#### Scenario: Demo user cannot modify tenant name
- **WHEN** a request to `PUT /api/tenant` is made with a demo JWT
- **THEN** a 403 Forbidden response is returned

#### Scenario: Demo user cannot change marina operator account details
- **WHEN** a request to update an operator user's profile is made with a demo JWT
- **THEN** a 403 Forbidden response is returned

#### Scenario: Demo user can create a booking
- **WHEN** a request to `POST /api/bookings` is made with a demo JWT
- **THEN** the booking is created and a 201 response is returned

#### Scenario: Demo user can record a payment
- **WHEN** a request to `POST /api/payments` is made with a demo JWT
- **THEN** the payment is recorded and a 201 response is returned

### Requirement: Demo tenant has pre-seeded realistic data
The demo tenant SHALL be seeded with realistic marina data including at least: one marina, multiple docks, multiple slips (a mix of occupied and available), several customer accounts, existing bookings, and at least one maintenance request.

#### Scenario: Demo operator session shows a populated dashboard
- **WHEN** an operator demo session is started
- **THEN** the operator dashboard displays at least one marina, at least five slips, and at least one existing booking

#### Scenario: Demo customer session shows a slip assignment
- **WHEN** a customer demo session is started
- **THEN** the customer portal displays an assigned slip and at least one invoice

### Requirement: Demo data resets on a schedule
The system SHALL reset all mutable demo tenant data (bookings, payments, maintenance requests, announcements) on a configurable schedule (default: every 2 hours) via a background job, then re-seed from the canonical seed script.

#### Scenario: Mutable demo data is cleared and re-seeded
- **WHEN** the demo reset job fires
- **THEN** all bookings, payments, maintenance requests, and announcements in the demo tenant are deleted and replaced with the canonical seed data

#### Scenario: Demo tenant structure is preserved after reset
- **WHEN** the demo reset job fires
- **THEN** the demo tenant record, marina configuration, dock/slip layout, and demo user accounts remain unchanged

#### Scenario: Reset interval is configurable
- **WHEN** `DemoResetIntervalMinutes` is set in application configuration
- **THEN** the Hangfire recurring job uses that interval instead of the default 120 minutes

### Requirement: Demo UI banner
The SaaS frontend SHALL display a persistent banner in demo sessions indicating the visitor is in demo mode, with a link to sign up or contact sales.

#### Scenario: Banner appears in all demo sessions
- **WHEN** the SaaS app is loaded with a demo JWT
- **THEN** a non-dismissible banner appears at the top of every page stating the user is in demo mode

#### Scenario: Banner contains a CTA to the marketing site
- **WHEN** the demo banner is visible
- **THEN** it includes a link back to the marketing site (e.g., "Learn more" or "Contact us")

### Requirement: "Try Demo" entry point on the marketing site
The marketing site SHALL include a prominent "Try Demo" button on the landing page (and navigation) that initiates a demo session for the operator persona by default, with an option to switch to the customer persona.

#### Scenario: Clicking "Try Demo" starts an operator demo session
- **WHEN** the visitor clicks the primary "Try Demo" button
- **THEN** `POST /api/demo/session?role=operator` is called and the visitor is redirected to the SaaS app operator dashboard

#### Scenario: Visitor can choose to demo as a customer
- **WHEN** a secondary "Try as Customer" option is visible near the primary CTA
- **THEN** clicking it calls `POST /api/demo/session?role=customer` and redirects to the SaaS app customer portal
