## 1. Infrastructure — Email Abstraction

- [x] 1.1 Add `IEmailService` interface to `MyMarina.Application.Abstractions` with methods: `SendCustomerInviteAsync`, `SendStaffInviteAsync`, `SendEmailConfirmationAsync`
- [x] 1.2 Create `EmailOptions` record in `MyMarina.Infrastructure/Email/` with properties: `Provider`, `Host`, `Port`, `SecureSocket` (string → `SecureSocketOptions`, default `"StartTls"`), `Username`, `Password`, `FromAddress`, `FromName`, `RequireConfirmedEmail`, `AppBaseUrl`
- [x] 1.3 Implement `NullEmailService` in `MyMarina.Infrastructure/Email/` — logs debug message, returns `Task.CompletedTask`
- [x] 1.4 Add `MailKit` and `MimeKit` NuGet packages to `MyMarina.Infrastructure`

## 2. Infrastructure — SMTP2GO Implementation

- [x] 2.1 Create `Smtp2GoEmailService` in `MyMarina.Infrastructure/Email/` implementing `IEmailService` using `MailKit.Net.Smtp.SmtpClient`
- [x] 2.2 Add HTML email templates as string constants in `MyMarina.Infrastructure/Email/Templates/` for: customer invite, staff invite, email confirmation
- [x] 2.3 Implement `SendCustomerInviteAsync` — builds MimeMessage with customer invite template, sends via SMTP
- [x] 2.4 Implement `SendStaffInviteAsync` — builds MimeMessage with staff invite template (includes role + marina name), sends via SMTP
- [x] 2.5 Implement `SendEmailConfirmationAsync` — builds MimeMessage with confirmation link only (no password), sends via SMTP

## 3. DI Registration & Configuration

- [x] 3.1 Add `Email` section to `appsettings.json` with `"Provider": "null"` and placeholder fields
- [x] 3.2 Add `Email` section to `appsettings.Production.json` with `"Provider": "smtp2go"` and env-var references (`${EMAIL_USERNAME}`, etc.)
- [x] 3.3 Register `EmailOptions` via `services.Configure<EmailOptions>` in `Infrastructure` DI extension method
- [x] 3.4 Add provider-selection logic: if `Provider == "smtp2go"` → register `Smtp2GoEmailService`, else → register `NullEmailService`
- [x] 3.5 Add startup validation: if `Provider == "smtp2go"` and any required field is null/empty, throw `InvalidOperationException` with a descriptive message

## 4. Identity — Email Confirmation Flow (Backend)

- [x] 4.1 Enable `options.SignIn.RequireConfirmedEmail` in `IdentityOptions` — guard it behind `EmailOptions.RequireConfirmedEmail`
- [x] 4.2 Add `GET /auth/confirm-email` endpoint in `AuthController` that accepts `userId` and `token` query params and calls `UserManager.ConfirmEmailAsync`
- [x] 4.3 Return HTTP 200 on success, HTTP 400 on invalid/expired token; make 200 idempotent for already-confirmed users

## 5. Invite Flows — Wire Email Sends

- [x] 5.1 Update `InviteCustomerCommandHandler`: after user creation, call `UserManager.GenerateEmailConfirmationTokenAsync`, build confirmation URL, call `IEmailService.SendCustomerInviteAsync` inside try/catch (non-fatal)
- [x] 5.2 Update `InviteStaffCommandHandler`: after user creation, call `UserManager.GenerateEmailConfirmationTokenAsync`, build confirmation URL, call `IEmailService.SendStaffInviteAsync` inside try/catch (non-fatal)

## 6. Frontend — Confirm Email Page

- [x] 6.1 Add `/confirm-email` route to TanStack Router (public route, outside operator/portal shells)
- [x] 6.2 Implement `ConfirmEmailPage` component: on mount, call `GET /api/auth/confirm-email?userId=&token=` using URL query params
- [x] 6.3 Show success state ("Your email has been confirmed — you can now log in") or error state ("Confirmation link is invalid or has expired") based on API response

## 7. Tests

- [x] 7.1 Ensure `WebApplicationFactory` registers `NullEmailService` as `IEmailService` (override in test `Program.cs` setup)
- [x] 7.2 Add integration test: `POST /customers/{id}/invite` — assert 201, assert confirmation token was generated (verify `EmailConfirmed == false` before confirmation)
- [x] 7.3 Add integration test: `GET /auth/confirm-email` — valid token → 200, `EmailConfirmed == true`
- [x] 7.4 Add integration test: `GET /auth/confirm-email` — invalid token → 400
- [x] 7.5 Add integration test: customer portal login blocked when `EmailConfirmed == false` and `RequireConfirmedEmail == true`
- [x] 7.6 Run full test suite (`dotnet test`) and confirm all existing tests still pass

## 8. API Types & Codegen

- [x] 8.1 Start the API and run `npm run generate-api` (requires Docker/Postgres running) in `src/MyMarina.Web/` to regenerate `schema.d.ts` with the new `/auth/confirm-email` endpoint
