## Why

The current customer invite flow has three critical issues: (1) operators are asked to re-enter customer email and name even though this data exists in the database, creating friction and data duplication, (2) newly invited customers cannot access their slips or invoices after login due to a broken link between the ApplicationUser and CustomerAccount, and (3) the system assumes a 1:1 relationship between users and customer accounts, blocking multi-tenant operators or users with multiple roles.

## What Changes

- Operators select an existing customer from a list during invite instead of manually entering email/name
- Fix the customer account context resolution so portal queries correctly filter to the invited customer's data
- Introduce support for users with multiple customer account memberships (same user can be in multiple accounts)
- Enable users to be members of multiple tenants (future-proofing for multi-tenant operators)
- Pre-populate the temporary password generation on invite and return it securely

## Capabilities

### New Capabilities

- `customer-selection-on-invite`: Operators select an existing customer account from a list instead of entering email/name manually
- `multi-customer-account-membership`: A single ApplicationUser can have multiple CustomerAccountMember records, enabling access to multiple customer accounts within the same tenant
- `multi-tenant-user-support`: Architectural foundation to support users being members of multiple tenants (implementation of cross-tenant context switching deferred to Phase 6)

### Modified Capabilities

- `customer-invite-flow`: The POST /customers/{id}/invite endpoint now expects only an existing customer account selection (no email/name input). Returns temporary password. The backend verifies the customer already has no associated user before creating one.
- `portal-authentication`: JWT claims now include all CustomerAccountIds the user belongs to (not just one). Portal queries filter by current CustomerAccountId context (resolved from URL/session) plus TenantId.

## Impact

- **Backend**: Customer invite endpoint contract change (no email/name in request). CustomerAccountMember becomes a join table supporting multiple memberships. JWT claims structure changes. Portal query handlers updated to handle multi-account scenarios.
- **Frontend**: Customer list modal replaces email/name input form on invite. Customer Portal navigation updated to support account switching if user has multiple accounts.
- **Database**: CustomerAccountMember may need a unique constraint update; ApplicationUser.CustomerAccountId removal if using JWT approach instead.
- **API**: POST /customers/{id}/invite signature changes; OpenAPI regeneration required.
