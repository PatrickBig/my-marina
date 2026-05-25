## 1. Backend: Data Models & Queries

- [x] 1.1 Create `GetUserProfileQuery` record in `MyMarina.Application/Platform/PlatformOperatorQueries.cs`
- [x] 1.2 Create `UserProfileDto` with nested Vessel/Reservation/Membership/AuditLog DTOs in `PlatformOperatorDtos.cs`
- [x] 1.3 Implement `GetUserProfileQueryHandler` in `MyMarina.Infrastructure/Platform/PlatformOperatorQueryHandlers.cs` with eager-loaded relationships (Vessels, Reservations, Memberships, recent AuditLog entries)
- [ ] 1.4 Add database indexes on `ApplicationUser.FirstName`, `ApplicationUser.LastName`, and `ApplicationUser.Email` if they don't exist (optional, for phase 2 if performance needed)
- [x] 1.5 Enhance `SearchUsersQueryHandler` to search by FirstName, LastName, Email, and PhoneNumber (case-insensitive)

## 2. Backend: Command Handlers for Account Modifications

- [x] 2.1 Create `ChangeUserEmailCommand` record in `MyMarina.Application/Platform/PlatformOperatorCommands.cs`
- [x] 2.2 Implement `ChangeUserEmailCommandHandler` that:
  - Validates email format (standard email regex)
  - Checks email uniqueness via `UserManager.FindByEmailAsync` (case-insensitive)
  - Updates `ApplicationUser.Email` via `UserManager.UpdateAsync`
  - Marks email as confirmed
  - Creates AuditLog entry with action "user.email_changed" and sanitized details
  - Returns error if email already in use

- [x] 2.3 Create `ChangeUserNameCommand` record accepting optional FirstName and LastName
- [x] 2.4 Implement `ChangeUserNameCommandHandler` that:
  - Validates that at least one field is provided and non-empty
  - Updates FirstName and/or LastName on ApplicationUser
  - Creates separate AuditLog entries for each field changed (action: "user.first_name_changed" or "user.last_name_changed")
  - Returns success/error response

- [x] 2.5 Create `InitiatePasswordResetCommand` record in `PlatformOperatorCommands.cs`
- [x] 2.6 Implement `InitiatePasswordResetCommandHandler` that:
  - Generates password reset token via `UserManager.GeneratePasswordResetTokenAsync`
  - Constructs reset URL (configure frontend URL in appsettings)
  - Sends email via existing `IEmailService` (async via Hangfire pattern)
  - Revokes all active refresh tokens for the user (use `ForceSignOutCommand` pattern)
  - Creates AuditLog entry with action "user.password_reset_requested" (no token in Details)
  - Returns success response

## 3. Backend: API Endpoints

- [x] 3.1 Add endpoint `GET /platform/users/{userId:guid}` to `PlatformOperatorController` that calls `GetUserProfileQuery` and returns `UserProfileDto`
- [x] 3.2 Add endpoint `PATCH /platform/users/{userId:guid}/email` that accepts `{ newEmail: string }` and calls `ChangeUserEmailCommand`
- [x] 3.3 Add endpoint `PATCH /platform/users/{userId:guid}/name` that accepts `{ firstName?: string, lastName?: string }` and calls `ChangeUserNameCommand`
- [x] 3.4 Add endpoint `POST /platform/users/{userId:guid}/password-reset` that calls `InitiatePasswordResetCommand`
- [x] 3.5 Add authorization checks (`RequireOperator()` guard) to all new endpoints
- [x] 3.6 Add OpenAPI response types to all endpoints (200, 400, 403, 404 as applicable)
- [ ] 3.7 Test endpoints manually using Scalar UI or curl to ensure correct responses

## 4. Backend: Audit Logging & DTOs

- [x] 4.1 Update `AuditLogEntryDto` if needed to include ActorName field (already has ActorUserId; query handler should fetch user name for display)
- [x] 4.2 Ensure all new command handlers follow audit logging pattern (Action, TargetType, TargetId, Details with sanitized values)
- [x] 4.3 Create integration test for audit log entries: verify email change creates audit entry with old/new email in Details
- [x] 4.4 Create integration test for password reset: verify audit entry created with action "user.password_reset_requested" (no token logged)

## 5. Backend: Testing

- [x] 5.1 Create integration test for `GetUserProfileQuery` with user that has vessels and reservations
- [x] 5.2 Create integration test for email change: success case, email already in use, invalid format
- [x] 5.3 Create integration test for name change: success with first name, last name, both fields
- [x] 5.4 Create integration test for password reset: token generation, email sent, refresh tokens revoked
- [x] 5.5 Create integration test for authorization: non-operator cannot access endpoints (403)
- [x] 5.6 Create integration test for enhanced user search: search by email, first name, last name, partial matches

## 6. Frontend: API Types & Setup

- [x] 6.1 Run `npm run generate-api` from `src/MyMarina.Web/` to regenerate TypeScript types after backend changes
- [x] 6.2 Verify `UserProfileDto`, `UserSummaryDto`, and new DTOs appear in `src/api/schema.d.ts`
- [x] 6.3 Update API client hooks if needed (e.g., `useQuery` for fetching user profile, `useMutation` for modifications)

## 7. Frontend: User Profile Page

- [x] 7.1 Create route `/admin/users/:userId` (or similar based on existing routing)
- [x] 7.2 Create `UserProfilePage.tsx` component that:
  - Fetches user profile via `GetUserProfileQuery` (with TanStack Query)
  - Displays personal details (name, email, status, confirmation, last login, created date)
  - Displays vessels list with name, type, dimensions
  - Displays reservations grouped by status (upcoming, past, cancelled)
  - Displays memberships with marina/tenant name and role
  - Shows activity timeline (recent audit log entries)
  - Loading spinner while fetching
  - Error message if user not found (404)

- [x] 7.3 Create action buttons on user profile: "Change Email", "Change Name", "Send Password Reset", "Force Sign Out", "Deactivate/Activate"
- [x] 7.4 Style page to match existing admin UI (use existing components, Tailwind CSS)

## 8. Frontend: Email Change Dialog

- [x] 8.1 Create `ChangeEmailDialog.tsx` modal/dialog component with form:
  - Display current email
  - Input field for new email
  - Validation: required, valid email format
  - Submit/Cancel buttons
  - Loading state during submission

- [x] 8.2 Implement confirmation dialog after form submission: "Change email from X to Y?"
- [x] 8.3 Call `PATCH /platform/users/{userId}/email` on confirm
- [x] 8.4 Show success toast/message, refresh user profile, close dialog on success
- [x] 8.5 Show error toast with server error message on failure
- [x] 8.6 Test: valid email, invalid format, email already in use, network error handling

## 9. Frontend: Name Change Dialog

- [x] 9.1 Create `ChangeNameDialog.tsx` modal/dialog with form:
  - Input fields for first name and last name (both optional, but at least one required)
  - Display current names
  - Validation: at least one field filled, not empty strings
  - Submit/Cancel buttons
  - Loading state

- [x] 9.2 Implement confirmation dialog: "Change name from X Y to A B?"
- [x] 9.3 Call `PATCH /platform/users/{userId}/name` on confirm
- [x] 9.4 Show success toast, refresh profile, close dialog on success
- [x] 9.5 Show error toast on failure
- [x] 9.6 Test: change first only, last only, both, validation errors

## 10. Frontend: Password Reset Dialog

- [x] 10.1 Create `PasswordResetDialog.tsx` modal with:
  - Message explaining action: "Send a password reset email to [user email]?"
  - Warning: "This will invalidate all current sessions"
  - Confirm/Cancel buttons
  - Loading state

- [x] 10.2 Call `POST /platform/users/{userId}/password-reset` on confirm
- [x] 10.3 Show success message: "Password reset email sent to [email]"
- [x] 10.4 Close dialog, optionally refresh audit log on user profile
- [x] 10.5 Show error toast on failure
- [x] 10.6 Test: successful send, network error handling

## 11. Frontend: Enhanced User Search

- [x] 11.1 Update existing user search/list page to support multi-field search
- [x] 11.2 Modify search input to have placeholder text: "Search by name, email, or phone"
- [x] 11.3 Verify that enhanced `SearchUsersQuery` returns results for all field types
- [x] 11.4 Test search: by email, first name, last name, phone, partial matches, case-insensitive

## 12. Frontend: Audit Log Display

- [x] 12.1 Ensure user profile activity section displays recent audit log entries related to the user
- [x] 12.2 Format timestamps as relative (e.g., "2 hours ago") and absolute on hover
- [x] 12.3 Display operator name (not ID) in activity timeline entries
- [x] 12.4 Test: verify email change, password reset, name change entries appear in activity section

## 13. Frontend: User Search Results Enhancement

- [x] 13.1 Update user search result list to show name, email, status, email confirmed, last login (already in `UserSummaryDto`)
- [x] 13.2 Add click handler to user row to navigate to user profile page
- [x] 13.3 Add "View Profile" button or make entire row clickable
- [x] 13.4 Test: click through from search to profile

## 14. End-to-End Testing

- [x] 14.1 Manual flow: Search for user → Open profile → Change email → Verify audit log entry
- [x] 14.2 Manual flow: Open user profile → Change name → Verify audit log updated
- [x] 14.3 Manual flow: Open user profile → Send password reset → Check that email was sent (or verify in Hangfire logs)
- [x] 14.4 Manual flow: Verify non-operator cannot access any of the new endpoints (403)
- [x] 14.5 Manual flow: Verify all success/error messages display correctly
- [x] 14.6 Verify audit log is complete and sanitized (no passwords, tokens, card numbers in Details)

## 15. Documentation & Cleanup

- [x] 15.1 Update README or admin docs with new operator capabilities (optional for phase 1)
- [x] 15.2 Verify no console errors or warnings in browser dev tools
- [x] 15.3 Run `dotnet build` and `dotnet test` — all tests pass
- [x] 15.4 Run `npm run build` from `src/MyMarina.Web/` — no build errors
- [x] 15.5 Review code for any TODOs or FIXMEs that should be addressed
- [x] 15.6 Ensure DemoSeedScript includes sample audit log entries if demo tenant has users (verify audit is visible in demo)

## 16. Deployment Preparation

- [x] 16.1 Merge all code changes to main branch
- [x] 16.2 Run full CI/CD pipeline (tests, builds, etc.)
- [x] 16.3 Deploy to staging environment and perform final smoke test
- [x] 16.4 Verify endpoints are accessible and functional in staging
- [x] 16.5 Ready for production deployment (track in project management system)
