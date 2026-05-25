## Context

The platform currently has a `PlatformOperatorController` with basic user management (sign-out, deactivate/activate). Support teams need richer tools to troubleshoot account issues: finding users by multiple criteria, viewing their complete account state (vessels, reservations, memberships), and modifying account details (email, name, password reset).

**Current infrastructure in place:**
- `ApplicationUser` (via ASP.NET Identity) with FirstName, LastName, Email, IsActive, LastLoginAt
- `AuditLog` entity already integrated into command handlers
- `UserManager<ApplicationUser>` for identity operations
- `Vessel`, `Reservation`, `Membership` entities with navigation properties
- `IUserContext` for authorization checks
- Existing pattern of command handlers + query handlers

**Key constraints:**
- ASP.NET Identity enforces email uniqueness (case-insensitive)
- Password operations must go through `UserManager` API
- JWT tokens must be regenerated for email changes to ensure consistency
- Email sending relies on existing `IEmailService` or Hangfire messaging
- Multi-tenancy via `IUserContext` filters — platform operators see all users (no tenant filter)

## Goals / Non-Goals

**Goals:**
- Platform operators can find users by name (first/last), email, or phone with efficient queries
- Operators can view a comprehensive user profile with all related entities (vessels, reservations, memberships, activity)
- Operators can modify email and name with proper validation and uniqueness checks
- Operators can force password reset with automatic reset email delivery
- All operator actions are audited with sanitized details (no passwords, card numbers, PII)
- New endpoints follow existing patterns and integrate cleanly into the API
- Frontend matches existing admin UI patterns and provides intuitive workflows

**Non-Goals:**
- Account merging (deferred to future phase)
- Resource transfer between users (deferred to future phase)
- Bulk user operations (out of scope; assume single-user workflows)
- Custom email templates for reset emails (use existing email service or sensible defaults)
- Phone number storage (search by phone acceptable if field exists; if not, skip for phase 1)
- User permissions/role assignment (separate concern from account management)
- GDPR export or data deletion (compliance features, separate initiative)

## Decisions

### 1. API Endpoint Design

**Decision:** Extend `PlatformOperatorController` with RESTful endpoints following existing patterns.

**Endpoints:**
```
GET  /platform/users/{userId:guid}              — Get full user profile
PATCH /platform/users/{userId:guid}/email        — Change user email
PATCH /platform/users/{userId:guid}/name         — Change first/last name
POST  /platform/users/{userId:guid}/password-reset — Trigger password reset
```

Enhanced existing endpoint:
```
GET  /platform/users?q=<search>&page=1&pageSize=25  — Enhanced to search by name, email, or phone
```

**Rationale:** Consistent with existing controller structure, familiar patterns for API consumers, follows RESTful conventions.

**Alternative considered:** Separate `UserManagementController` for new endpoints — rejected because it splits related functionality and adds another controller to maintain.

---

### 2. User Profile Query Strategy

**Decision:** Create a new query handler `GetUserProfileQuery` that efficiently fetches user details, related vessels, reservations, memberships, and recent audit log entries in a single query (or minimal queries).

**Pattern:**
```csharp
public record GetUserProfileQuery(Guid UserId);
public record UserProfileDto(
    UserSummaryDto User,
    List<VesselDto> Vessels,
    List<ReservationDto> Reservations,
    List<MembershipDto> Memberships,
    List<AuditLogEntryDto> RecentActivity);
```

**Rationale:** Operators need complete context when troubleshooting — requiring multiple round-trips (separate requests for vessels, reservations, etc.) creates poor UX and wastes API calls. Single query ensures consistency (snapshot view of user at one point in time).

**Implementation:** Use EF Core `.Include()` chains to eagerly load relationships; apply global query filters for multi-tenancy (none for platform operators). Consider pagination for RecentActivity if list grows large.

**Alternative considered:** Separate endpoints for each resource — rejected for reasons above.

---

### 3. Email Change Implementation

**Decision:** When operator changes email:
1. Validate new email format and uniqueness (case-insensitive) using `UserManager.FindByEmailAsync`
2. Update `ApplicationUser.Email` via `UserManager`
3. Mark email as confirmed (operator is trusted authority)
4. Audit log entry with old and new email (no sensitive data in Details)
5. DO NOT force sign-out (user session remains valid; email is not part of JWT claims in your design)

**Code structure:**
```csharp
public class ChangeUserEmailCommand { Guid TargetUserId, string NewEmail }
public class ChangeUserEmailCommandHandler : ICommandHandler<ChangeUserEmailCommand>
```

**Rationale:** ApplicationUser.Email is writable via UserManager. Confirm email since operator verified it. Session doesn't need to be invalidated because email is not part of the JWT; JWT contains `sub` (user ID) and `email` claim, but email is informational, not authz-critical.

**Risk:** If email is used elsewhere for identity (e.g., password reset token generation), regeneration might be needed. Check how email is used in password reset flow.

**Alternative considered:** Auto-send confirmation email to new address — rejected because operator is acting as trusted authority and confirmation email adds UX friction.

---

### 4. Name Change Implementation

**Decision:** Separate commands for first/last name changes (or single command accepting both).

**Option A (chosen):** Single `ChangeUserNameCommand` accepting optional FirstName and LastName. Only update fields that are provided (non-null).

**Code structure:**
```csharp
public class ChangeUserNameCommand { Guid TargetUserId, string? FirstName, string? LastName }
public class ChangeUserNameCommandHandler : ICommandHandler<ChangeUserNameCommand>
```

Audit log entries:
- If FirstName changed: `Action = "user.first_name_changed"`, Details include old/new
- If LastName changed: `Action = "user.last_name_changed"`, Details include old/new
- If both: Two separate audit entries

**Rationale:** Allows operator to fix either field independently. Separate audit entries maintain clarity (one action per field change). Validation: both fields required to be non-empty.

**Alternative considered:** Separate endpoints for first/last — rejected as over-engineered; single command with optional fields is cleaner.

---

### 5. Password Reset Flow

**Decision:** Use ASP.NET Identity's built-in password reset token system:
1. Operator calls `POST /platform/users/{userId}/password-reset`
2. Backend calls `UserManager.GeneratePasswordResetTokenAsync(user)`
3. Construct reset URL: `{frontend-url}/auth/reset-password?userId={userId}&token={token}`
4. Send email via existing `IEmailService` or Hangfire
5. Revoke all refresh tokens (ForceSignOutCommand pattern already exists)
6. Audit log entry: `Action = "user.password_reset_requested"` (no token in Details)

**Rationale:** Leverages Identity's proven token system; doesn't reinvent password reset. Token generation is scoped to user, time-sensitive, and already validated by Identity.

**Email flow:** Async via Hangfire (consistent with existing patterns) so endpoint returns quickly.

**Session invalidation:** Yes — revoke all refresh tokens so user cannot continue using old session. New login required after password reset.

**Risk:** Email delivery is eventual-consistent (async). If email system fails, operator won't know. Consider logging to audit with status or implementing delivery confirmation webhook later.

**Alternative considered:** Operator-generated temporary password — rejected because it's less secure (operator sees password, manual distribution error-prone) and increases security audit surface.

---

### 6. Enhanced User Search

**Decision:** Modify existing `SearchUsersQuery` to support searching by FirstName, LastName, Email, or Phone (if field exists on ApplicationUser).

**Implementation:**
```csharp
public record SearchUsersQuery(string? Q, int Page = 1, int PageSize = 25);
```

Query handler implements:
```csharp
var query = db.Users.AsQueryable();
if (!string.IsNullOrWhiteSpace(q))
{
    var pattern = q.ToLower();
    query = query.Where(u =>
        u.Email.ToLower().Contains(pattern) ||
        u.FirstName.ToLower().Contains(pattern) ||
        u.LastName.ToLower().Contains(pattern) ||
        (u.PhoneNumber != null && u.PhoneNumber.Contains(pattern)));
}
return await query.Paginate(page, pageSize).ToListAsync();
```

**Rationale:** Simple, covers most search scenarios. Case-insensitive matching. Single query.

**Performance note:** At scale (1M+ users), a LIKE query on Name/Email columns could be slow. Consider adding database indexes on FirstName, LastName, Email if search latency becomes an issue (optimize later based on metrics).

**Alternative considered:** Elasticsearch or full-text search — rejected for phase 1 (complexity, added infrastructure). SQL LIKE is acceptable for initial volume.

---

### 7. Audit Logging Pattern

**Decision:** Follow existing pattern from `PlatformOperatorCommandHandlers`:
- Each command handler creates an `AuditLog` entry after successful operation
- Action name: `"user.<operation>"` (e.g., `"user.email_changed"`, `"user.password_reset_requested"`)
- Details: Human-readable description, never include passwords/tokens/card numbers
- ActorUserId: From `IUserContext.UserId`
- TargetId: User ID as string
- TargetType: `"User"`

**Example:**
```csharp
db.AuditLogs.Add(new AuditLog
{
    ActorUserId = user.UserId,
    Action = "user.email_changed",
    TargetType = "User",
    TargetId = command.TargetUserId.ToString(),
    Details = $"Changed email from {oldEmail} to {newEmail}",
});
```

**Rationale:** Consistent with existing code. Audit log is already seeded and queryable. Details are human-readable for operators.

**Constraint:** Never log sensitive values (passwords, reset tokens, card numbers). For password reset, log only that the action was initiated, not the token.

---

### 8. Frontend Architecture

**Decision:** Create new routes/pages under platform admin:
- `/admin/users` — Enhanced user search and list (already exists, enhance this)
- `/admin/users/:userId` — New user profile detail page
- Modal/drawer for action buttons (change email, change name, password reset)

**State management:** Use existing patterns (TanStack Query for server state, React Hook Form for forms, Zustand if local UI state needed).

**Loading states:** Show spinners during API calls, disable buttons during submission, show confirmation dialogs before destructive actions.

**Rationale:** Minimizes new patterns; reuses existing tooling. Familiar to existing frontend developers.

---

## Risks / Trade-offs

| Risk | Mitigation |
|------|-----------|
| **Email uniqueness under concurrent edits** — Two operators change same user's email to different addresses simultaneously. Identity constraint prevents both from succeeding, but error handling must be graceful. | Implement transactional update with proper constraint checking. Return clear error message to operator: "Email is already in use or was recently changed." Operator retries with different email. |
| **Password reset email delivery failure** — Email system is down or misconfigured. Operator doesn't know reset wasn't sent. User never receives link. | Log email sending status to audit log or a separate delivery log. (Optional for phase 1: implement webhook or polling for delivery confirmation in phase 2.) For now, rely on Hangfire retry logic. |
| **Phone number field missing on ApplicationUser** — Specs mention searching by phone, but the field may not exist yet. | Check if `PhoneNumber` field exists on ApplicationUser (it's part of IdentityUser<T> base class). If it does, implement search. If operators aren't populating it, search by phone won't work. Document in release notes. Can defer phone search to later phase if field is unused. |
| **Performance at scale (1M+ users)** — SQL LIKE search on Name/Email becomes slow without indexes. | Add database indexes on FirstName, LastName, Email if latency monitoring shows issues. No action needed for initial phase. Monitor query performance. |
| **Case sensitivity in email matching** — Email should be case-insensitive per RFC 5321. ASP.NET Identity handles this, but custom queries must use `.ToLower()`. | Ensure all email comparisons in code use case-insensitive matching (`.ToLower()` or database collation). Test with mixed-case inputs. |
| **JWT claim consistency after email change** — If email is embedded in JWT claims, changing it while user has active session causes claim mismatch. | Verify JWT design: if email is a claim, invalidate all sessions (force sign-out) on email change. Per CLAUDE.md, JWT contains `email` claim but it's informational. If not auth-critical, no session invalidation needed. Confirm with team. |
| **Audit log query performance** — If audit log grows large (100K+ entries), filtering/searching becomes slow. | Implement pagination (already spec'd). Add database indexes on ActorUserId, TargetId, OccurredAt if filtering becomes bottleneck. Phase 1 concern: just ensure pagination is implemented. |
| **Confirmation dialogs UX** — If operators habitually click through confirmations, they might accidentally change wrong user's data. | Clear, specific dialog text: "Change email for John Doe from old@example.com to new@example.com?" Require operator to type confirmation if very destructive (e.g., password reset). Phase 1: standard confirmation dialog is sufficient. |

---

## Migration Plan

**Deployment:**
1. Backend: Merge code changes to main, deploy API to staging/prod (no database migrations needed)
2. Frontend: Build and deploy React bundle alongside API
3. No breaking changes to existing endpoints (enhanced search is backward compatible)
4. Audit log entries for old operations (existing ForceSignOut, Deactivate) are already in schema

**Rollback:** Remove new endpoint routes from controller, revert code. No database cleanup needed (audit log entries remain for historical record).

**Feature flag:** (Optional) Can wrap new endpoints in feature flag if gradual rollout desired. Phase 1: assume immediate enablement for platform operators.

---

## Open Questions

1. **Phone number field** — Does `ApplicationUser.PhoneNumber` exist and is it populated by users? If not used, should we skip phone search or add it later?
2. **Email change behavior** — Should we force sign-out the user when operator changes their email? (Currently: no, unless JWT design requires it.) Confirm JWT schema.
3. **Password reset email template** — Use default Identity email template or custom? Who maintains email templates?
4. **Email delivery monitoring** — Do we need delivery confirmation before confirming to operator that reset was sent? (Phase 1: assume Hangfire fire-and-forget is acceptable.)
5. **Activity timeline granularity** — Should "activity" on user profile include all audit log entries, or only specific events (logins, reservations, vessels created)? How many entries to show?
