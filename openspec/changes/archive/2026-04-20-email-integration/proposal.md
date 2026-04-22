## Why

MyMarina sends temporary passwords for invites and account creation but delivers them only as API response bodies — operators must relay credentials manually, creating friction and a security anti-pattern. Adding a provider-backed email layer enables secure delivery of invite credentials, identity verification, and future transactional notifications (invoices, maintenance updates, etc.).

## What Changes

- **New `IEmailService` abstraction** in `MyMarina.Application.Abstractions` — a single interface all email sends go through, making provider swap (SMTP2GO → Amazon SES) a one-line DI change.
- **SMTP2GO provider implementation** in `MyMarina.Infrastructure` — sends via SMTP2GO's SMTP relay using `MailKit`.
- **ASP.NET Core Identity email confirmation** — on registration/invite, `UserManager` generates an email confirmation token; a confirmation link is emailed to the user. Portal login is blocked until confirmed (configurable).
- **Invite emails for all invite flows** — customer invite, staff invite, and future operator provisioning all trigger a templated email with a confirmation/set-password link instead of (or alongside) returning a plaintext password.
- **Email template system** — simple Razor-based or string-interpolated HTML templates for each email type; stored in Infrastructure, easy to extend.
- **Configuration** — SMTP2GO credentials (`Host`, `Port`, `Username`, `Password`, `FromAddress`, `FromName`) read from `appsettings` / environment variables. Feature flag to disable email in development/test.

## Capabilities

### New Capabilities

- `email-service`: Provider-agnostic `IEmailService` abstraction and SMTP2GO implementation — the core email infrastructure all other sends route through.
- `identity-email-verification`: ASP.NET Core Identity email confirmation flow — token generation, confirmation endpoint, and enforcement on login.
- `invite-email-delivery`: Sending templated invite emails for customer, staff, and operator invite flows, replacing manual password relay.

### Modified Capabilities

- `customer-invite-flow`: The invite response continues to return the temporary password (for operator reference) **AND** now triggers an invite email to the customer's address with a confirmation link.

## Impact

- **New dependency:** `MailKit` NuGet package in `MyMarina.Infrastructure`.
- **Configuration:** New `Email` section in `appsettings.json` / environment variables; integration tests skip email send (mock `IEmailService`).
- **Auth flow:** `POST /portal/login` (customer) gains an optional "email not confirmed" guard — returns `403` with `email_not_confirmed` error code if enforcement is enabled.
- **Identity:** `ApplicationUser.EmailConfirmed` (already on `IdentityUser`) is leveraged; no schema migration needed.
- **Invite endpoints affected:** `POST /customers/{id}/invite`, `POST /staff/invite` — both gain side-effect email sends.
- **New endpoint:** `GET /auth/confirm-email?userId=&token=` for clicking the confirmation link.
