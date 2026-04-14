using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Common;
using MyMarina.Application.Customers;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Customers;

public class GetCustomerAccountsQueryHandler(AppDbContext db) : IQueryHandler<GetCustomerAccountsQuery, IReadOnlyList<CustomerAccountDto>>
{
    public async Task<IReadOnlyList<CustomerAccountDto>> HandleAsync(GetCustomerAccountsQuery query, CancellationToken ct = default)
    {
        return await db.CustomerAccounts
            .OrderBy(a => a.DisplayName)
            .Select(a => new CustomerAccountDto(
                a.Id,
                a.DisplayName,
                a.BillingEmail,
                a.BillingPhone,
                a.BillingAddress != null
                    ? new AddressDto(a.BillingAddress.Street, a.BillingAddress.City, a.BillingAddress.State, a.BillingAddress.Zip, a.BillingAddress.Country)
                    : null,
                a.EmergencyContactName,
                a.EmergencyContactPhone,
                a.Notes,
                a.IsActive,
                a.CreatedAt))
            .ToListAsync(ct);
    }
}
