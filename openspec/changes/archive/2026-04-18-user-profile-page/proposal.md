## Why

Users currently have no way to view or update their own account information after signing up. A profile page gives users self-service control over their contact details, email address, and password — reducing support burden and improving trust.

## What Changes

- New **Profile** page accessible to all authenticated users (operators and customers) at `/profile`
- Users can update their display name and contact information
- Users can change their email address (requires re-verification or confirmation step)
- Users can change their password (requires current password for verification)
- Profile data sourced from `ApplicationUser` (ASP.NET Core Identity) and the active `UserContext`

## Capabilities

### New Capabilities

- `user-profile`: View and update personal account information (display name, contact details, email, password)

### Modified Capabilities

<!-- None — this is additive only; no existing spec-level behavior changes -->

## Impact

- **Backend:** New API endpoints (`GET /profile`, `PUT /profile`, `POST /profile/change-password`, `POST /profile/change-email`)
- **Frontend:** New `/profile` route accessible from both operator and portal layouts; link in nav/user menu
- **Identity:** Uses ASP.NET Core Identity `UserManager` for email and password updates
- **Auth:** Requires authenticated JWT; no tenant/marina scoping needed — profile is per-user
