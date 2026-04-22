## Context

MyMarina currently handles three invite flows (`POST /customers/{id}/invite`, `POST /staff/invite`) by returning a plaintext temporary password in the API response body. The operator must copy and relay this credential manually. ASP.NET Core Identity's `EmailConfirmed` flag on `IdentityUser` is already in the schema but unused. SMTP2GO is the chosen SMTP provider (account + domain already verified).

The system has no email infrastructure at all today — no `IEmailService`, no templates, no configuration section.

## Goals / Non-Goals

**Goals:**
- Introduce a provider-agnostic `IEmailService` abstraction so the concrete sender can be swapped (SMTP2GO → SES) without touching call sites.
- Implement SMTP2GO via `MailKit` SMTP client.
- Wire ASP.NET Core Identity email confirmation: generate token on invite, email confirmation link, enforce confirmed-email on customer portal login.
- Send invite emails on all existing invite endpoints (customer + staff); include the temporary password and a confirmation link.
- Provide a `NullEmailService` for integration tests and local dev to prevent real sends.

**Non-Goals:**
- HTML email design system / marketing templates.
- Asynchronous email queuing via Hangfire (fire-and-forget inline send is acceptable for MVP).
- Bounce/complaint handling, unsubscribe management.
- Amazon SES implementation (abstraction makes this a future one-liner).
- Email preferences / opt-out management.

## Decisions

### 1. `IEmailService` lives in `MyMarina.Application.Abstractions`

**Decision:** Define a single `IEmailService` interface in the Application layer with strongly-typed `Send*` methods (e.g., `SendInviteEmailAsync`, `SendStaffInviteEmailAsync`, `SendEmailConfirmationAsync`).

**Rationale:** Application-layer handlers need to send email without depending on Infrastructure. A domain-generic `Send(EmailMessage)` alternative was considered but rejected — named methods make the call sites self-documenting and prevent callers from constructing ad-hoc messages incorrectly.

**Alternatives considered:** A generic `SendAsync(to, subject, body)` overload — kept as an internal helper on the concrete implementation only, not exposed via the interface.

### 2. `MailKit` over `System.Net.Mail.SmtpClient`

**Decision:** Use `MailKit` (`MailKit` + `MimeKit` NuGet) for SMTP delivery.

**Rationale:** `SmtpClient` is marked obsolete in .NET docs and lacks async support + STARTTLS reliability. MailKit is the community-recommended replacement, supports OAuth2 in future, and SMTP2GO's own docs reference it.

### 2a. Explicit `SecureSocket` config option in `EmailOptions`

**Decision:** Add a `SecureSocket` string field to `EmailOptions` that maps to MailKit's `SecureSocketOptions` enum: `"None"`, `"Auto"`, `"SslOnConnect"` (port 465, implicit TLS), or `"StartTls"` (port 587, explicit STARTTLS). Default to `"StartTls"`.

**Rationale:** SMTP2GO supports both port 465 (SSL) and port 587 (STARTTLS). Amazon SES and other providers have the same split. Using `SecureSocketOptions.Auto` feels convenient but silently falls back to plain-text if TLS negotiation fails — an unacceptable security regression. An explicit config value makes the intent clear and fails loudly if misconfigured. `"StartTls"` + port 587 matches SMTP2GO's recommended settings and is the safest default for a shared-relay provider.

**Practical config:**

```json
"Email": {
  "Provider": "smtp2go",
  "Host": "mail.smtp2go.com",
  "Port": 587,
  "SecureSocket": "StartTls",
  "Username": "...",
  "Password": "..."
}
```

### 3. HTML templates as embedded resource strings in Infrastructure

**Decision:** Store email templates as C# string constants (or embedded `.html` resource files) inside `MyMarina.Infrastructure/Email/Templates/`. No Razor rendering engine.

**Rationale:** Razor email rendering requires additional scaffolding (`IViewRenderService`, no `HttpContext`, etc.). For MVP with ~3 email types, parameterised string interpolation is simpler, has zero extra dependencies, and is easy to replace later with a proper template engine (Fluid, Scriban) if needed.

### 4. Email confirmation enforcement is configurable

**Decision:** A boolean `Email:RequireConfirmedEmail` config key (default `true` in Production, `false` in Development). The portal login guard reads this flag via `IOptions<EmailOptions>`.

**Rationale:** Allows local dev and integration tests to skip confirmation without code changes. Identity's own `RequireConfirmedEmail` option on `IdentityOptions.SignIn` can enforce this at the `SignInManager` level — use that rather than a manual guard.

### 5. Invite endpoints continue to return the temporary password

**Decision:** `POST /customers/{id}/invite` and `POST /staff/invite` still return the generated password in the response body **and** send an email. The password is not removed from the response.

**Rationale:** Operators may onboard customers face-to-face. Keeping the password in the response preserves that workflow. Email delivery is a best-effort addition, not a replacement.

### 6. `NullEmailService` registered in test/dev, `Smtp2GoEmailService` in production

**Decision:** Register `IEmailService` via a keyed or conditional DI registration based on `Email:Provider` config value (`"smtp2go"` | `"null"`). Default to `"null"` when config section is absent.

**Rationale:** Integration tests must not send real emails. Rather than mocking at the test level, a no-op implementation registered via config is cleaner and matches how the payment provider abstraction is designed.

## Risks / Trade-offs

- **Email delivery failure is silent** → The invite endpoint sends email inline; if SMTP2GO is unreachable the exception will bubble up and the entire invite fails. Mitigation: Catch `SmtpException` and log a warning — the invite succeeds, email failure is non-fatal. Move to Hangfire background job in a future iteration.
- **Temporary password in email body** → Sending plaintext credentials via email is a known security trade-off. Mitigation: The email also includes a "set your password" link. Post-MVP, remove the plaintext password from email and use the confirmation token exclusively.
- **No email delivery confirmation** → SMTP2GO accepts the message but we have no webhook for bounces. Mitigation: SMTP2GO dashboard provides delivery logs; add webhook integration as post-MVP.
- **Configuration drift** → `appsettings.json` may not have the `Email` section in all environments. Mitigation: Fail fast on startup with a clear exception if `Email:Provider` is `"smtp2go"` and required fields (`Host`, `Username`, `Password`, `FromAddress`) are missing.

## Migration Plan

1. Add `MailKit` NuGet to `MyMarina.Infrastructure`.
2. Add `Email` configuration section to `appsettings.json` (with `"Provider": "null"`) and `appsettings.Production.json` (with `"Provider": "smtp2go"` and real credentials via environment variables / secrets).
3. Add `IEmailService` to `MyMarina.Application.Abstractions`.
4. Implement `NullEmailService` and `Smtp2GoEmailService` in `MyMarina.Infrastructure/Email/`.
5. Register via DI in `Program.cs`.
6. Update `InviteCustomerCommandHandler` and `InviteStaffCommandHandler` to inject `IEmailService` and call the appropriate send method.
7. Enable `RequireConfirmedEmail` on `IdentityOptions.SignIn` (guarded by config).
8. Add `GET /auth/confirm-email` endpoint.
9. Update integration tests to assert email send was attempted (via spy/null service) where relevant.

**Rollback:** Remove `Email:RequireConfirmedEmail: true` from production config — confirmed-email gate is disabled, system behaves as today. No DB migration to roll back.

## Open Questions

- Should the confirmation link point to the React SPA (`/confirm-email?token=...`) which then calls the API, or directly to the API endpoint? → Recommend: SPA route that proxies to API, so we can show a branded confirmation page.
- Should staff invites also require email confirmation before first login, or just customers? → Recommendation: Yes for both, but staff confirmation timeout can be longer (7 days vs 24 hours for customers).
