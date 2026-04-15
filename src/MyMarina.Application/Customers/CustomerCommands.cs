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
