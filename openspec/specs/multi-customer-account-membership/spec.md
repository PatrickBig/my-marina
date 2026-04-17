## ADDED Requirements

### Requirement: ApplicationUser can belong to multiple CustomerAccounts
The system SHALL support a single ApplicationUser being a member of multiple CustomerAccount records via the CustomerAccountMember join table, enabling future scenarios where a user manages multiple accounts.

#### Scenario: User is member of one account
- **WHEN** a customer account is invited and a user created
- **THEN** one CustomerAccountMember record exists with (UserId, CustomerAccountId, role=Owner)

#### Scenario: User becomes member of additional account
- **WHEN** another CustomerAccount invites the same user (same email)
- **THEN** a second CustomerAccountMember record is created for the same UserId
- **AND** both accounts are accessible via that user's JWT claims

#### Scenario: JWT claims include all customer account IDs
- **WHEN** a user with multiple CustomerAccountMember records logs in
- **THEN** the JWT claim `customer_account_ids` contains an array of all CustomerAccountId values
- **AND** the portal can determine which accounts the user can access

#### Scenario: Duplicate membership rejected
- **WHEN** attempting to add the same user to the same account twice
- **THEN** the system rejects with HTTP 409 Conflict
- **AND** the unique constraint on (UserId, CustomerAccountId) prevents insertion
