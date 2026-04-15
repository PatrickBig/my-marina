using MyMarina.Application.Abstractions;
using MyMarina.Application.Common;

namespace MyMarina.Application.Customers;

public sealed record GetCustomerAccountsQuery;
public sealed record GetCustomerAccountQuery(Guid CustomerAccountId);

public sealed record CustomerAccountDto(
    Guid Id,
    string DisplayName,
    string BillingEmail,
    string? BillingPhone,
    AddressDto? BillingAddress,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    string? Notes,
    bool IsActive,
    DateTimeOffset CreatedAt);

public interface IGetCustomerAccountsQueryHandler : IQueryHandler<GetCustomerAccountsQuery, IReadOnlyList<CustomerAccountDto>>;
public interface IGetCustomerAccountQueryHandler : IQueryHandler<GetCustomerAccountQuery, CustomerAccountDto?>;
