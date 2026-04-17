## ADDED Requirements

### Requirement: Database and auth layer support users across tenants
The system's identity and authorization layers SHALL be structured to support users belonging to multiple tenants, even though Phase 5 UX/operators remain single-tenant. This is a foundation for Phase 6 multi-tenant operator feature.

#### Scenario: No artificial tenant restriction on ApplicationUser
- **WHEN** a user record is created
- **THEN** the ApplicationUser entity contains no TenantId foreign key
- **AND** user tenancy is derived from role and context (customer portal vs. operator)

#### Scenario: Operator context uses ITenantContext
- **WHEN** an operator logs in
- **THEN** ITenantContext resolves from JWT claims (or URL for multi-tenant operators in Phase 6)
- **AND** all operator queries filter by TenantId via EF global filter

#### Scenario: Customer context uses IMarinaContext with CustomerAccountId
- **WHEN** a customer logs in
- **THEN** IMarinaContext.CustomerAccountId is set from JWT claim `customer_account_ids` (currently single-selected)
- **AND** portal queries filter by both TenantId (EF global) and CustomerAccountId (explicit WHERE)

#### Scenario: Unique constraint on CustomerAccountMember
- **WHEN** creating a CustomerAccountMember
- **THEN** a unique constraint on (UserId, CustomerAccountId) ensures no duplicates
- **AND** this allows future N:N relationships without schema changes
