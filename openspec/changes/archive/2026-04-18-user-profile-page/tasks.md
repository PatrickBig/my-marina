## 1. Backend — Application Abstractions

- [x] 1.1 Create `GetProfileQuery` record and `GetProfileResult` DTO in `MyMarina.Application`
- [x] 1.2 Create `UpdateProfileCommand` record in `MyMarina.Application`
- [x] 1.3 Create `ChangeEmailCommand` record in `MyMarina.Application`
- [x] 1.4 Create `ChangePasswordCommand` record in `MyMarina.Application`

## 2. Backend — Infrastructure Handlers

- [x] 2.1 Create `MyMarina.Infrastructure/Profile/` directory
- [x] 2.2 Implement `GetProfileQueryHandler` — reads `ApplicationUser` via `UserManager` using `UserId` from `IHttpContextAccessor`
- [x] 2.3 Implement `UpdateProfileCommandHandler` — updates `UserName`/`PhoneNumber` via `UserManager`
- [x] 2.4 Implement `ChangeEmailCommandHandler` — verifies current password, checks email uniqueness, calls `UserManager.SetEmailAsync`; returns conflict on duplicate
- [x] 2.5 Implement `ChangePasswordCommandHandler` — calls `UserManager.ChangePasswordAsync`; returns validation errors on failure
- [x] 2.6 Register all four handlers in `ServiceCollectionExtensions` (or Infrastructure DI setup)

## 3. Backend — API Controller

- [x] 3.1 Create `ProfileController` at `MyMarina.Api/Controllers/ProfileController.cs`
- [x] 3.2 Add `GET /profile` endpoint — requires `[Authorize]`, dispatches `GetProfileQuery`
- [x] 3.3 Add `PUT /profile` endpoint — requires `[Authorize]`, validates + dispatches `UpdateProfileCommand`
- [x] 3.4 Add `POST /profile/change-email` endpoint — requires `[Authorize]`, validates + dispatches `ChangeEmailCommand`; returns 409 on conflict
- [x] 3.5 Add `POST /profile/change-password` endpoint — requires `[Authorize]`, validates + dispatches `ChangePasswordCommand`
- [x] 3.6 Add FluentValidation validators for `UpdateProfileCommand`, `ChangeEmailCommand`, and `ChangePasswordCommand`

## 4. Backend — Tests

- [x] 4.1 Add integration tests for `GET /profile` (authenticated, unauthenticated)
- [x] 4.2 Add integration tests for `PUT /profile` (valid update, empty display name)
- [x] 4.3 Add integration tests for `POST /profile/change-email` (success, duplicate email, wrong password, bad format)
- [x] 4.4 Add integration tests for `POST /profile/change-password` (success, wrong current password, policy violation, same password)

## 5. Frontend — API Types

- [x] 5.1 Ensure API is running, then run `npm run generate-api` to regenerate `src/api/schema.d.ts` with the new profile endpoints

## 6. Frontend — Profile Page

- [x] 6.1 Create `src/pages/ProfilePage.tsx` with three sections: Personal Info form, Change Email form, Change Password form
- [x] 6.2 Implement Personal Info form — fields: display name, phone; pre-filled from `GET /profile`; submits `PUT /profile`
- [x] 6.3 Implement Change Email form — fields: new email, current password; submits `POST /profile/change-email`; shows 409 error inline
- [x] 6.4 Implement Change Password form — fields: current password, new password, confirm new password; submits `POST /profile/change-password`; shows validation errors inline
- [x] 6.5 Add success/error toast feedback for all three forms

## 7. Frontend — Routing & Navigation

- [x] 7.1 Add `/profile` route to the TanStack Router config (absolute path, protected, accessible from both layouts)
- [x] 7.2 Add "Profile" link to the operator layout user menu (top-right dropdown or nav)
- [x] 7.3 Add "Profile" link to the customer portal layout user menu
- [x] 7.4 Verify unauthenticated access to `/profile` redirects to login
