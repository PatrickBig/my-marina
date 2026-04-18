## Context

MyMarina uses ASP.NET Core Identity (`ApplicationUser`) for authentication. Users are identified by a `UserId` (Guid) embedded in their JWT. There is currently no UI or API surface for a user to read or update their own account details. All identity operations (email/password changes) must go through ASP.NET Core Identity's `UserManager<ApplicationUser>`.

The profile feature must work for **all** authenticated user types: platform operators, tenant operators, marina staff, and customers. It is not tenant- or marina-scoped — it is purely per-user.

## Goals / Non-Goals

**Goals:**
- Expose `GET /profile` to return the current user's display name, email, and phone
- Expose `PUT /profile` to update display name and phone number
- Expose `POST /profile/change-email` to update email (with current-password confirmation)
- Expose `POST /profile/change-password` to change password (requires current password)
- Add a `/profile` page in the React SPA accessible from both operator and portal layouts
- Reuse existing JWT auth middleware — no new auth scheme needed

**Non-Goals:**
- Email verification flow (out of scope for MVP; email change is accepted immediately)
- Two-factor authentication setup
- Profile photo / avatar upload
- OAuth / social login linking
- Notification preferences

## Decisions

### 1. Single shared `/profile` route (not layout-specific)

Both operator layout and portal layout will link to the same `/profile` absolute path. The page fetches the current user from the JWT and has no tenant/marina dependency, so it needs no layout duplication.

**Alternative considered:** Separate `/portal/profile` and `/profile`. Rejected — unnecessary duplication and confusing UX.

### 2. Current password required for email and password changes

Both `change-email` and `change-password` require the user's current password as a security confirmation step, using `UserManager.CheckPasswordAsync` before applying the change.

**Alternative considered:** Email confirmation link for email changes. Deferred — email sending not configured in MVP.

### 3. Controllers pattern — new `ProfileController`

Consistent with the existing codebase pattern (one controller per feature area). Uses `ICommandHandler` / `IQueryHandler` from `MyMarina.Application.Abstractions`.

### 4. Handlers in Infrastructure layer

Profile read/write touches `ApplicationUser` (Identity), which lives in Infrastructure. Handlers go in `MyMarina.Infrastructure.Profile/` alongside other Infrastructure handlers.

## Risks / Trade-offs

- **Email uniqueness collision** → `UserManager.SetEmailAsync` returns an `IdentityResult`; handler must surface a 409 Conflict if the email is already taken.
- **Password policy enforcement** → `UserManager.ChangePasswordAsync` enforces the configured `PasswordOptions`; return 400 with validation errors on failure.
- **No email verification** → Users can set any email address without confirming ownership. Acceptable for MVP since product is not live.
