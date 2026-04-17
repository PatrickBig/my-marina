using MyMarina.Application.Abstractions;
using MyMarina.Application.Common;

namespace MyMarina.Application.Customers;

public sealed record CreateCustomerAccountCommand(
    string DisplayName,
    string BillingEmail,
    string? BillingPhone,
    AddressDto? BillingAddress,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    string? Notes);

public sealed record UpdateCustomerAccountCommand(
    Guid CustomerAccountId,
    string DisplayName,
    string BillingEmail,
    string? BillingPhone,
    AddressDto? BillingAddress,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    string? Notes);

public sealed record DeactivateCustomerAccountCommand(Guid CustomerAccountId);

public interface ICreateCustomerAccountCommandHandler : ICommandHandler<CreateCustomerAccountCommand, Guid>;
public interface IUpdateCustomerAccountCommandHandler : ICommandHandler<UpdateCustomerAccountCommand>;
public interface IDeactivateCustomerAccountCommandHandler : ICommandHandler<DeactivateCustomerAccountCommand>;

/// <summary>
/// Creates a portal user linked to an existing CustomerAccount.
/// The account must exist and not yet have a user. Email/name come from CustomerAccount.
/// Returns a temporary password the marina operator shares out-of-band.
/// </summary>
public sealed record InviteCustomerCommand(Guid CustomerAccountId);

public sealed record InviteCustomerResult(Guid UserId, string TemporaryPassword);

public interface IInviteCustomerCommandHandler : ICommandHandler<InviteCustomerCommand, InviteCustomerResult>;
