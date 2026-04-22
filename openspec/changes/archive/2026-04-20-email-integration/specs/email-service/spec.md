## ADDED Requirements

### Requirement: IEmailService abstraction in Application layer
The system SHALL provide an `IEmailService` interface in `MyMarina.Application.Abstractions` with named async methods for each transactional email type. All email sends in the application MUST route through this interface.

#### Scenario: Interface is resolved from DI
- **WHEN** a command handler requests `IEmailService` from the DI container
- **THEN** the container resolves the registered implementation without error

#### Scenario: Named methods cover all transactional email types
- **WHEN** `IEmailService` is defined
- **THEN** it SHALL expose at minimum: `SendCustomerInviteAsync`, `SendStaffInviteAsync`, `SendEmailConfirmationAsync`

### Requirement: SMTP2GO implementation using MailKit
The system SHALL provide a `Smtp2GoEmailService` that sends email via SMTP using `MailKit`. It SHALL read connection details from `IOptions<EmailOptions>` (`Host`, `Port`, `Username`, `Password`, `FromAddress`, `FromName`).

#### Scenario: Successful email delivery
- **WHEN** `Smtp2GoEmailService.SendCustomerInviteAsync` is called with valid parameters
- **THEN** the service opens an authenticated SMTP connection to the configured host
- **AND** transmits the message
- **AND** closes the connection

#### Scenario: Missing required configuration
- **WHEN** the application starts with `Email:Provider` set to `"smtp2go"` and `Email:Host` is absent
- **THEN** startup MUST fail with a descriptive configuration exception before any requests are served

### Requirement: NullEmailService no-op implementation
The system SHALL provide a `NullEmailService` that implements `IEmailService` by logging a debug-level message and returning without sending anything.

#### Scenario: NullEmailService does not throw
- **WHEN** any `IEmailService` method is called on `NullEmailService`
- **THEN** the call completes successfully with no side effects and no exceptions

#### Scenario: NullEmailService is registered when provider is absent or null
- **WHEN** the `Email` config section is absent or `Email:Provider` is `"null"`
- **THEN** `NullEmailService` is registered as the `IEmailService` implementation

### Requirement: Provider selection via configuration
The system SHALL select the `IEmailService` implementation at startup based on the `Email:Provider` configuration value (`"smtp2go"` or `"null"`).

#### Scenario: SMTP2GO selected in production
- **WHEN** `Email:Provider` is `"smtp2go"` and all required fields are present
- **THEN** `Smtp2GoEmailService` is registered as `IEmailService`

#### Scenario: Null provider is the default
- **WHEN** `Email:Provider` is absent or set to `"null"`
- **THEN** `NullEmailService` is registered as `IEmailService`
