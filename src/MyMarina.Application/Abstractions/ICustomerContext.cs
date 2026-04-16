namespace MyMarina.Application.Abstractions;

/// <summary>
/// Provides the current customer's account ID resolved from JWT claims.
/// Only valid for requests authenticated with the Customer role.
/// </summary>
public interface ICustomerContext
{
    Guid CustomerAccountId { get; }
    bool IsCustomer { get; }
}
