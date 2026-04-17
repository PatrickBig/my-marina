## Context

Currently, the invite flow (`POST /customers/{id}/invite`) requires operators to enter customer email and name manually, even though these come from existing `CustomerAccount` records. The backend creates both `ApplicationUser` and `CustomerAccountMember` (Owner role), but the customer portal cannot access their data post-login due to misaligned context resolution. Additionally, the database and JWT model assume 1:1 user-to-account mapping, preventing users from being members of multiple accounts or tenants.

## Goals / Non-Goals

**Goals:**
- Eliminate redundant data entry: operators select from a list of existing customer accounts
- Fix portal data access: ensure `CustomerAccountMember` records correctly authorize portal queries
- Enable multi-account membership: allow one user to belong to multiple `CustomerAccount` records
- Foundation for multi-tenant users: structure that supports users across different tenants (deferred implementation)
- Preserve security: no cross-customer or cross-tenant data leaks

**Non-Goals:**
- UI for users to switch between accounts (Phase 6)
- Full multi-tenant operator switching (Phase 6)
- Password generation UX improvements beyond temporary password return

## Decisions

**Decision 1: Remove direct `ApplicationUser.CustomerAccountId` foreign key**
- **Rationale**: A 1:1 foreign key blocks multi-account membership. `CustomerAccountMember` is already the authoritative join table.
- **Alternative considered**: Keep FK and allow nullable, but adds complexity and ambiguity about "primary" account.
- **Migration**: Remove the column from `ApplicationUser`. Portal queries rely solely on `CustomerAccountId` in JWT claims (resolved from `CustomerAccountMember` records at login).

**Decision 2: JWT claims include all `CustomerAccountId` values user belongs to**
- **Rationale**: Client needs to know which accounts the user can access; server can switch context per-request.
- **Format**: `customer_account_ids` claim as JSON array of GUIDs. For single-account users, this is a 1-element array.
- **Alternative considered**: Store only current account; requires API call to get others. Rejected due to UX friction.
- **Implementation**: In `LoginCommand` handler, query `CustomerAccountMembers` for the user; extract all unique `CustomerAccountId` values. Set as JWT claim.

**Decision 3: Portal queries use both `TenantId` (EF global filter) and current `CustomerAccountId` (explicit WHERE)**
- **Rationale**: EF global filter ensures tenant isolation; explicit `CustomerAccountId` filter prevents cross-account access within same tenant.
- **Current account resolution**: `IMarinaContext.CustomerAccountId` property (already exists). Ensure all portal query handlers reference it.
- **Alternative considered**: Store `CustomerAccountId` in EF global filter. Rejected; global filters are per-query-type, but we need per-request context.

**Decision 4: Front-end: replace email/name form with `CustomerAccount` list modal**
- **Rationale**: Operators already know which customer they're inviting; showing existing accounts eliminates redundant entry.
- **Data flow**: GET `/customers` returns all accounts for the marina; modal displays `{ name, email, id }` for selection. POST `/customers/{id}/invite` payload now contains only selected `CustomerAccountId` (implicit from path).
- **Validation**: Backend checks: account exists, belongs to invoking operator's marina, account has no user yet (1:1 user-per-account for now, even though membership supports multiple).

**Decision 5: Keep 1:1 user-per-account constraint in invite logic (even though schema supports N:N)**
- **Rationale**: Phase 4 scope constraint; customers expect one login per account. Multi-account access is foundation, but UI/behavior remains single-account in Phase 5.
- **Enforcement**: Invite handler checks `CustomerAccountMembers.Where(c => c.CustomerAccountId == selectedId).Any(m => m.User != null)`. Reject if user exists.
- **Migration note**: This is not schema-enforced (no unique constraint), so future phases can lift it without schema change.

## Risks / Trade-offs

**[Risk] JWT claim size grows with account membership**
- **Mitigation**: In practice, single operators rarely exceed 5–10 accounts; claim size remains < 500 bytes. If this becomes an issue, defer to Phase 6 session table lookup.

**[Risk] Existing integrations expect `ApplicationUser.CustomerAccountId` field**
- **Mitigation**: Audit codebase (grep for `.CustomerAccountId`) before removal. Update any references to use JWT claim instead.

**[Risk] Operator invites customer already invited elsewhere**
- **Mitigation**: Unique constraint on `(UserId, CustomerAccountId)` in `CustomerAccountMembers` prevents duplicates. Invite handler catches and returns 409 Conflict if user is already a member.

**[Risk] Portal queries forget to filter by `CustomerAccountId`**
- **Mitigation**: Code review checklist: all portal handlers (those in `MyMarina.Infrastructure.Portal`) must reference `_customerContext.CustomerAccountId` in WHERE clause. Consider query analyzer decorator to enforce at runtime.

## Migration Plan

1. **Schema**: Remove `ApplicationUser.CustomerAccountId` column (EF migration).
2. **Backend**: Update `LoginCommand` to extract all `CustomerAccountMember` records and emit `customer_account_ids` JWT claim.
3. **Backend**: Update `InviteCustomerCommandHandler` to require `CustomerAccountId` (remove email/name fields). Add duplicate check.
4. **Backend**: Audit and update all portal query handlers to use `_customerContext.CustomerAccountId` in filters.
5. **Frontend**: Replace email/name invite form with account selection modal. Post to `/customers/{selectedAccountId}/invite` (no body).
6. **API**: Regenerate OpenAPI schema (`npm run generate-api`).
7. **Test**: Integration tests for multi-account login, portal access with correct account, invite duplicate rejection.

## Open Questions

- Should temporary password be returned in response body or via email? (Current: in response body for MVP). Confirm with product.
- If operator mistakenly invites wrong customer, can/should they revoke and re-invite? (Deferred to Phase 6.)
- Should we email the temp password in addition to returning it? (Deferred; security decision for Phase 6.)
