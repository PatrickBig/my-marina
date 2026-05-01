# MyMarina — Authentication & Permissions

> Cross-references: [overview.md](./overview.md) for the personas, [data-model.md](./data-model.md) for the identity entities, [glossary.md](./glossary.md) for terminology.

## Identity Strategy

MyMarina uses **ASP.NET Core Identity** for user storage, password hashing, lockout, email confirmation, and 2FA scaffolding — combined with **custom JWT issuance** for API authentication. We deliberately do **not** use `MapIdentityApi<T>()`.

### Why not `MapIdentityApi`?

`MapIdentityApi` is .NET 8+'s built-in `/register`, `/login`, `/refresh` endpoint set. It works for simple use cases, but it doesn't fit ours:

- It issues opaque bearer tokens (cookie-style) by default, not JWTs. Customizing the token format is not officially supported.
- It cannot embed custom claims into the token, and our authorization model needs `memberships`, `billing_accounts`, and `slip_ownership` claims baked in.
- It doesn't extend cleanly to social login (Google, Apple, Facebook) — external providers are linked through Identity's standard challenge/callback flow, not through `MapIdentityApi`'s endpoints.
- It doesn't support custom logic on registration (terms acceptance, marketing opt-in, profile completion gates).
- Refresh-token rotation is fixed; we want to rotate when permissions change.

### What we use instead

Custom controllers in `MyMarina.Api/Controllers/AuthController.cs` (planning, not yet built). They sit on top of Identity's `UserManager<ApplicationUser>` and `SignInManager<ApplicationUser>`, which give us for free:

- Password hashing (`PasswordHasher<T>`)
- Account lockout
- Email confirmation token generation/validation
- External login linking (`UserManager.AddLoginAsync`)
- 2FA scaffolding
- `IUserStore`, `IUserClaimStore`, `IUserLoginStore` abstractions if we ever swap storage

The custom layer adds:

- JWT issuance with our claim shape
- Refresh-token rotation policy with permission-change invalidation
- Shaped login responses (user profile + accessible memberships in one round-trip)
- Hooks for terms acceptance and post-signup workflows (welcome email, claim-pending-vessel)

---

## Login Flows

### Email + password

| Endpoint | Method | Purpose |
| --- | --- | --- |
| `/auth/register` | POST | Creates `ApplicationUser`; sends confirmation email |
| `/auth/login` | POST | Validates credentials; issues access + refresh JWT |
| `/auth/refresh` | POST | Rotates access and refresh tokens |
| `/auth/logout` | POST | Revokes the caller's refresh token |
| `/auth/forgot-password` | POST | Triggers Identity's password-reset email |
| `/auth/reset-password` | POST | Validates the reset token; sets new password |
| `/auth/confirm-email` | POST | Validates the email-confirmation token |
| `/auth/resend-confirmation` | POST | Re-sends the confirmation email |

### Social login (Google, Apple, Facebook)

| Endpoint | Method | Purpose |
| --- | --- | --- |
| `/auth/external/{provider}` | GET | Initiates OAuth challenge; provider ∈ `google`, `apple`, `facebook` |
| `/auth/external/{provider}/callback` | GET | Handles OAuth redirect; signs in or registers |
| `/auth/external/{provider}/link` | POST | Links a provider to the currently signed-in account |
| `/auth/external/{provider}/unlink` | POST | Removes a linked provider |

Provider packages:

- `Microsoft.AspNetCore.Authentication.Google`
- `Microsoft.AspNetCore.Authentication.Apple` (.NET 8+, official)
- `Microsoft.AspNetCore.Authentication.Facebook`

### Account linking rules

A single `ApplicationUser` may have any number of external logins linked plus an optional email/password.

| Scenario | Behavior |
| --- | --- |
| First-time external login, new email | Create new `ApplicationUser`, link external identity, mark email confirmed (provider has verified it), issue JWT |
| First-time external login, email matches existing user | Require user to sign in with the existing method first, then link from their profile page (prevents account-takeover via email-collision) |
| Multiple external providers | Allowed — a user can link Google + Apple + Facebook to the same account |
| External login with no email | Apple, in particular, can return private relay emails. Treat as confirmed; user can add a recovery email later |

A user with no email/password and only external logins is fully supported. They cannot use `/auth/login`; they must use `/auth/external/...`.

---

## JWT Claim Shape

Issued JWTs carry these claims:

```json
{
  "sub": "01HX...",                 // user id (UUID v7)
  "email": "user@example.com",
  "email_verified": true,
  "given_name": "Patrick",
  "family_name": "Bigler",
  "platform_role": "Operator",      // present only for platform staff
  "memberships": "[ { ... } ]",     // JSON-encoded; see below
  "billing_accounts": "[ ... ]",    // JSON-encoded
  "iat": 1714200000,
  "exp": 1714203600,
  "jti": "01HX..."
}
```

### `memberships` claim

JSON-encoded array of the user's accepted `Membership` records:

```json
[
  { "scope": "Marina", "marina_id": "01HX...", "tenant_id": "01HX...", "role": "Owner",   "tier": "Pro" },
  { "scope": "Tenant", "tenant_id": "01HX...",                          "role": "Owner",   "tier": "Premium" },
  { "scope": "Marina", "marina_id": "01HX...", "tenant_id": "01HX...", "role": "Staff",   "tier": "Free" }
]
```

`tier` is denormalized in for `[RequiresTier]` checks so the policy handler doesn't need a DB hit.

### `billing_accounts` claim

JSON-encoded array of accepted `BillingAccountMember` records:

```json
[
  { "billing_account_id": "01HX...", "marina_id": "01HX...", "role": "Owner" },
  { "billing_account_id": "01HX...", "marina_id": "01HX...", "role": "Member" }
]
```

### Slip ownership

There is no separate slip-ownership claim. **Slip permissions resolve through `Membership` at `Slip.MarinaId`.** This holds for all real-world ownership cases — commercial marina, yacht club, dockominium, private dock — because the platform pins every slip to a Marina (single-slip personal marinas auto-created for private-host owners). Dockominium-host visibility/approval flows through `Membership` at `Slip.HostMarinaId` plus the `HostMarinaPolicy`.

### Claim size considerations

Realistic claim sizes stay well under 2 KB:

| User profile | `memberships` entries | Approx. JWT size |
| --- | --- | --- |
| Boater only | 0 | ~0.7 KB |
| Boater + customer at 2 marinas | 0 + 2 billing accounts | ~1.0 KB |
| Private-dock owner | 1 (their personal marina) | ~0.9 KB |
| Marina staff at 1 marina | 1 | ~0.9 KB |
| Marina-chain owner (50 marinas, 1 tenant) | 1 (Tenant scope) | ~1.0 KB |
| Marina staff at 3 unrelated tenants | 3 | ~1.4 KB |

Tenant-scoped memberships collapse marina chains to a single entry. The pathological case (Owner roles at 30 unrelated marinas) still fits under 5 KB; HTTP headers tolerate up to 8 KB. If size ever becomes a problem, we'll switch to lightweight JWT + claim transformation (`IClaimsTransformation`) to lazy-load permissions on first authenticated request.

---

## Authorization Model

Authorization uses ASP.NET Core's **policy** system. Custom `IAuthorizationHandler` implementations parse the membership claims and decide whether the request is allowed.

### Policies

```text
Policies (planning, not yet implemented):
  PlatformOperator         — global IdentityRole "PlatformOperator"
  marina:owner             — user has Owner Membership on the route's marinaId (or Tenant Owner of that marina's tenant)
  marina:manager           — Owner or Manager
  marina:staff             — Owner, Manager, or Staff
  billing:owner            — user has Owner BillingAccountMember role
  billing:member           — user has any BillingAccountMember role
  reservation:participant  — user is BoaterUserId, OR has marina:staff at Slip.MarinaId (or its host marina, where applicable)
```

### Endpoint shape

Endpoints that need a marina-scoped check use route-bound authorization:

- `[Authorize(Policy = Policies.MarinaStaff)]` on `GET /marinas/{marinaId}/customers` — handler reads `marinaId` from the route, checks the JWT's `memberships` claim for an entry that grants Staff+ at that marina (or any membership at that marina's tenant).

For nested resources (e.g., `GET /marinas/{marinaId}/billing-accounts/{billingAccountId}`), the handler additionally validates that the billing account belongs to the marina. This is one DB query per request, cached briefly.

### Tier gating

`[RequiresTier(SubscriptionTier.Pro)]` on a controller action checks the JWT's `memberships` claim for the relevant marina/tenant and rejects the request with HTTP 402 (Payment Required) if the tier is too low. Tier-to-feature assignments live in `TierCapabilityRegistry` — see the v0 implementation as a reference.

### Platform operators bypass

`PlatformOperator` global role bypasses all marina/tenant checks but is **always audit-logged** with a `platform_action` flag on the `AuditLog` entry. Platform operator actions never silently mutate tenant data without a trail.

---

## Refresh Tokens & Permission Rotation

Refresh tokens are stored server-side in the `RefreshToken` table (see [data-model.md](./data-model.md#refreshtoken)). Tokens are stored as SHA-256 hashes — the raw token never sits in the database.

### Standard rotation

| Action | Effect |
| --- | --- |
| `/auth/refresh` called with valid refresh token | Old token revoked (`RevokedAt` set, `ReplacedByTokenId` set); new access + refresh issued |
| `/auth/refresh` called with reused (already-rotated) token | All of the user's active refresh tokens revoked (token-reuse detection); user forced to re-login |
| `/auth/logout` | Caller's refresh token revoked |
| Refresh-token expiry (`ExpiresAt` reached) | Token can no longer be used; user re-logs in |

### Permission-change invalidation

When a user's permissions change, the system **revokes all of that user's active refresh tokens**. Triggers:

- A `Membership` is granted, accepted, role-changed, or removed
- A `BillingAccountMember` is granted, accepted, role-changed, or removed
- `User.IsActive` flips
- A `PlatformOperator` Identity role is granted or revoked
- A subscription `Tenant.SubscriptionTier` changes (so tier-gated capabilities reflect immediately)

On the next access-token expiry (default 1 hour), the client calls `/auth/refresh` and gets a 401. The client redirects to login. The user re-authenticates (which is silent for users on social-login auto-fill), and the new JWT carries up-to-date claims.

This is a deliberate trade-off:

- **Pro:** Permissions are always reflected within the access-token TTL.
- **Pro:** Tokens are stateless during their TTL — no per-request DB hit for permissions.
- **Con:** Forces a sign-in on permission change. Acceptable because permission changes are rare and the user re-authenticates through their normal login.

A future variant could re-issue silently using a long-lived "session-cookie" identifier separate from the JWT, but that's post-MVP.

### Token TTLs

| Token | Default TTL |
| --- | --- |
| Access token | 1 hour |
| Refresh token | 30 days (rolling — each refresh extends) |

---

## Email Verification

Email confirmation is **required** before a user can:

- Make a reservation
- Receive a vessel-claim invitation acceptance
- Accept a billing-account invitation
- Accept a marina membership invitation

Email confirmation is **not required** to:

- Browse public listings
- Search slips
- Add boats to their profile
- Save searches
- Sign in via a social provider that has already verified the email (auto-confirmed)

Hosts (marina staff and slip owners) must have a confirmed email before any host-side action.

---

## Two-Factor Authentication (Future)

Identity provides 2FA scaffolding (`UserManager.SetTwoFactorEnabledAsync`, `GenerateTwoFactorTokenAsync`, etc.). Wired up post-MVP for:

- Platform operators (mandatory)
- Marina owners (recommended; opt-in)
- Boaters (opt-in)

Authenticator-app TOTP first, SMS fallback later.

---

## Audit & Session Management

Every authentication event writes to `AuditLog`:

- `auth.register`, `auth.login`, `auth.logout`
- `auth.refresh`, `auth.refresh_reused` (token-reuse detection event)
- `auth.password_reset_requested`, `auth.password_reset`
- `auth.email_confirmed`
- `auth.external_linked`, `auth.external_unlinked`
- `auth.permission_invalidated` (refresh tokens revoked due to permission change)

Platform operators can:

- View any user's active refresh-token sessions
- Revoke all of a user's refresh tokens (force sign-out)
- View login history
- Disable a user (`User.IsActive = false`) — invalidates all tokens immediately

---

## Open Questions

These are deferred until implementation. Not blockers for the data model.

- **Anonymous browsing → reservation flow:** When an unauthenticated visitor finds a slip and clicks "Reserve," do we require sign-up before showing the booking form, or capture their booking details first and then prompt? Recommendation: prompt sign-up first (cleaner data).
- **Apple private relay emails:** Apple-issued private relay addresses change if the user revokes the relay. Handle gracefully on next sign-in (re-link by Apple subject ID, not by email).
- **Provider-initiated unlink:** What happens if a user revokes Google access from their Google account? On next attempt to sign in via Google, treat as new external login, prompt to re-link via password.
- **Concurrent logins:** Allow unlimited concurrent sessions, or cap at N? Default: unlimited; revisit if abuse appears.
