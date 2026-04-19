## ADDED Requirements

### Requirement: Hero section with clear value proposition
The marketing site SHALL display a hero section on the landing page that communicates the primary value proposition of MyMarina to prospective marina operators within 5 seconds of page load.

#### Scenario: Visitor lands on the marketing site
- **WHEN** a visitor navigates to the root domain
- **THEN** the hero section is visible above the fold with a headline, subheadline, and a primary CTA button ("Try the Demo")

#### Scenario: CTA button is visible without scrolling
- **WHEN** the page loads on a desktop viewport (≥1024px wide)
- **THEN** the "Try the Demo" CTA button is visible without any scrolling

### Requirement: Feature highlights section
The marketing site SHALL include a features section that describes at least three core capabilities of the platform, with one highlight per persona (marina operator, marina customer, platform overview).

#### Scenario: Features section renders below the hero
- **WHEN** the visitor scrolls past the hero
- **THEN** a features section appears with distinct cards or panels, each with an icon, headline, and one-sentence description

#### Scenario: Persona-specific framing is present
- **WHEN** the features section is rendered
- **THEN** at least one panel addresses marina operators and at least one addresses marina customers

### Requirement: Navigation header
The marketing site SHALL include a persistent navigation header with the MyMarina logo and links to key sections (Features, Demo, Contact).

#### Scenario: Navigation is present on all marketing pages
- **WHEN** any marketing page is loaded
- **THEN** the navigation header is visible at the top of the page with logo and section links

#### Scenario: Navigation links scroll to the correct section
- **WHEN** a visitor clicks a navigation link (e.g., "Features")
- **THEN** the page smoothly scrolls to the corresponding section

### Requirement: Contact / lead capture section
The marketing site SHALL include a contact section with a form or mailto link allowing prospects to request more information or a sales conversation.

#### Scenario: Contact section is reachable from navigation
- **WHEN** the visitor clicks "Contact" in the navigation header
- **THEN** the page scrolls to a contact section visible below the fold

#### Scenario: Contact form submits successfully
- **WHEN** the visitor fills in their name, email, and a message and clicks "Send"
- **THEN** the form is submitted and the visitor sees a confirmation message ("We'll be in touch!")

#### Scenario: Contact form validates required fields
- **WHEN** the visitor attempts to submit the form with an empty required field
- **THEN** an inline validation error is shown and the form is not submitted

### Requirement: Responsive layout for mobile and tablet
The marketing site SHALL be fully usable on mobile (≥375px) and tablet (≥768px) viewports with no horizontal overflow and readable typography.

#### Scenario: Mobile viewport renders without horizontal scroll
- **WHEN** the page is viewed on a 375px-wide viewport
- **THEN** no horizontal scrollbar appears and all content is readable

#### Scenario: Navigation collapses to a hamburger menu on mobile
- **WHEN** the page is viewed on a viewport narrower than 768px
- **THEN** the navigation links collapse into a hamburger menu icon

### Requirement: Marketing site is served at the root domain
The marketing site SHALL be deployed and accessible at the root domain (e.g., `mymarina.org` / `www.mymarina.org`), distinct from the SaaS application.

#### Scenario: Root domain serves the marketing site
- **WHEN** a user navigates to the root domain in a browser
- **THEN** the marketing landing page is returned (not the SaaS login screen)

#### Scenario: SaaS app is accessible at a subdomain
- **WHEN** a user navigates to `app.mymarina.org`
- **THEN** the SaaS application login page is returned

### Requirement: Page metadata for SEO
The marketing site SHALL include appropriate `<title>`, `meta description`, and Open Graph tags on the landing page.

#### Scenario: Landing page has a descriptive title and meta description
- **WHEN** the landing page HTML is fetched by a crawler
- **THEN** the `<title>` tag contains "MyMarina" and the product's core value proposition, and a `<meta name="description">` tag is present with content under 160 characters
