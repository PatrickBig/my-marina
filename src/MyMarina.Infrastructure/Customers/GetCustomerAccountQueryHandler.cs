using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Common;
using MyMarina.Application.Customers;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Customers;

public class GetCustomerAccountQueryHandler(AppDbContext db) : IQueryHandler<GetCustomerAccountQuery, CustomerAccountDto?>
{
    public async Task<CustomerAccountDto?> HandleAsync(GetCustomerAccountQuery query, CancellationToken ct = default)
    {
        var a = await db.CustomerAccounts.FirstOrDefaultAsync(x => x.Id == query.CustomerAccountId, ct);
        if (a is null) return null;

        return new CustomerAccountDto(
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
            a.CreatedAt);
    }
}
