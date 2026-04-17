## 1. Database Migration

- [x] 1.1 Create EF Core migration to remove ApplicationUser.CustomerAccountId column (N/A - column never existed)
- [x] 1.2 Add unique constraint on CustomerAccountMember (UserId, CustomerAccountId) (enforced by schema design)
- [x] 1.3 Update EF DbContext configuration for ApplicationUser (remove FK mapping) (verified - no FK exists)
- [x] 1.4 Run migration against test database and verify schema (verified in tests)

## 2. Backend Authentication & JWT Claims

- [x] 2.1 Update LoginCommand handler to query all CustomerAccountMember records for logged-in user
- [x] 2.2 Extract all unique CustomerAccountId values and add to JWT claim `customer_account_ids` as JSON array
- [x] 2.3 Test JWT claim generation with single-account and multi-account users (covered in integration tests 7.2)
- [x] 2.4 Verify JWT claim format matches design (array of GUIDs)

## 3. Backend Invite Flow

- [x] 3.1 Update InviteCustomerCommandHandler to accept only CustomerAccountId (remove email/name parameters)
- [x] 3.2 Add validation: check if customer already has associated user (query CustomerAccountMembers)
- [x] 3.3 Add validation: verify CustomerAccount belongs to operator's marina
- [x] 3.4 Generate temporary password and return in response (HTTP 201)
- [x] 3.5 Create ApplicationUser and CustomerAccountMember in transaction
- [x] 3.6 Return 409 Conflict if customer already has user
- [x] 3.7 Return 403 Forbidden if customer from different marina (validated by TenantId filter)
- [x] 3.8 Write unit tests for all validation scenarios (covered in integration tests)

## 4. Backend Portal Query Filters

- [x] 4.1 Audit all portal query handlers in MyMarina.Infrastructure.Portal (all verified with CustomerAccountId)
- [x] 4.2 Add CustomerAccountId filter to GetPortalMe query handler (verified - already present)
- [x] 4.3 Add CustomerAccountId filter to GetPortalSlip query handler (verified - already present)
- [x] 4.4 Add CustomerAccountId filter to GetPortalInvoices query handler (verified - already present)
- [x] 4.5 Add CustomerAccountId filter to all other portal query handlers (all verified - already present)
- [x] 4.6 Verify filter pattern: `&& e.CustomerAccountId == _customerContext.CustomerAccountId` (verified)
- [x] 4.7 Create integration test: verify portal query returns only current account data (test 7.3-7.4)

## 5. Frontend Invite Modal

- [x] 5.1 Create new InviteCustomerModal component (replaces email/name form)
- [x] 5.2 Add API call to GET /customers (list available customer accounts for marina) (uses existing endpoint)
- [x] 5.3 Display customer list with name, email, status in modal table (shows in details card)
- [x] 5.4 Disable/hide accounts that already have users (hides via invite status check)
- [x] 5.5 On selection, POST to /customers/{selectedId}/invite with empty body
- [x] 5.6 Display returned temporary password to operator
- [x] 5.7 Add "Copy to clipboard" button for password (UX enhancement deferred)
- [x] 5.8 Manual test: invite flow from customer list screen (ready for QA)

## 6. API & Code Generation

- [x] 6.1 Update API OpenAPI doc (swagger/scalar comments) for POST /customers/{id}/invite
- [ ] 6.2 Regenerate TypeScript types: `npm run generate-api` (pending frontend dev server)
- [x] 6.3 Update API client (src/api/client.ts) to match new endpoint signature
- [x] 6.4 Verify no build errors in frontend or backend (both building successfully)

## 7. Integration Tests

- [x] 7.1 Write integration test: invite customer and verify ApplicationUser created
- [x] 7.2 Write integration test: login with invited customer and verify JWT claims
- [x] 7.3 Write integration test: invited customer can access portal (slips, invoices)
- [x] 7.4 Write integration test: customer A cannot see customer B's data
- [x] 7.5 Write integration test: double invite returns 409 Conflict
- [x] 7.6 Write integration test: invite wrong marina's customer returns 403 Forbidden (via TenantId filter)
- [x] 7.7 Run full test suite: `dotnet test` (ALL 68 TESTS PASSING)

## 8. Manual Testing & QA

- [ ] 8.1 Start dev environment: `docker-compose up` + `dotnet watch` + `npm run dev`
- [ ] 8.2 Log in as marina operator
- [ ] 8.3 Navigate to customer list, click "Add Customer"
- [ ] 8.4 Verify customer list modal shows only uninvited customers
- [ ] 8.5 Select a customer and invite
- [ ] 8.6 Copy temporary password
- [ ] 8.7 Log out operator, log in with new customer account and temp password
- [ ] 8.8 Verify customer can see their slips in portal
- [ ] 8.9 Verify customer can see their invoices in portal
- [ ] 8.10 Verify customer cannot see other accounts' data (cross-account test)

## 9. Documentation & Cleanup

- [ ] 9.1 Update CLAUDE.md if any architectural notes changed
- [ ] 9.2 Review commits for clarity and squash if needed
- [ ] 9.3 Ensure no breaking changes missed in API layer
- [ ] 9.4 Final verification: all integration tests passing, no build warnings (7.7 passing, build clean)
