## ADDED Requirements

### Requirement: JWT includes array of customer account IDs
The system SHALL include a `customer_account_ids` claim in the JWT token issued to customer users. This claim contains an array of all CustomerAccountId values for which the user is a CustomerAccountMember.

#### Scenario: Single account customer receives array claim
- **WHEN** a customer user logs in
- **THEN** the JWT claim `customer_account_ids` is an array with one element
- **AND** example claim: `"customer_account_ids": ["550e8400-e29b-41d4-a716-446655440000"]`

#### Scenario: Multi-account customer receives full array
- **WHEN** a customer user with multiple CustomerAccountMember records logs in
- **THEN** the JWT claim `customer_account_ids` contains all associated account IDs
- **AND** example claim: `"customer_account_ids": ["id-1", "id-2", "id-3"]`

### Requirement: Portal queries filter by CustomerAccountId in addition to TenantId
The system SHALL ensure all portal query handlers filter results by the current CustomerAccountId context (from `IMarinaContext.CustomerAccountId`), in addition to the TenantId global filter, to prevent cross-customer data access.

#### Scenario: Portal query returns only current account data
- **WHEN** a portal API is called (e.g., GET /portal/slip, POST /portal/invoice)
- **THEN** results are filtered by both TenantId (EF global filter) and the current CustomerAccountId
- **AND** no data from other customer accounts is returned

#### Scenario: Missing CustomerAccountId filter rejected in code review
- **WHEN** a new portal query handler is added
- **THEN** it MUST include `&& c.CustomerAccountId == _customerContext.CustomerAccountId` or similar filter
- **AND** code review process flags handlers without this filter

### Requirement: IMarinaContext.CustomerAccountId indicates current account context
The system SHALL provide `IMarinaContext.CustomerAccountId` property that handlers use to determine which account's data to return.

#### Scenario: CustomerAccountId set from request context
- **WHEN** a portal request arrives
- **THEN** IMarinaContext.CustomerAccountId is resolved (currently from first array element in JWT claim)
- **AND** handlers use this value in WHERE clauses

#### Scenario: CustomerAccountId changed per-request
- **WHEN** different portal requests target different accounts
- **THEN** each request's IMarinaContext.CustomerAccountId reflects its target account
- **AND** queries in one request do not bleed data to another
