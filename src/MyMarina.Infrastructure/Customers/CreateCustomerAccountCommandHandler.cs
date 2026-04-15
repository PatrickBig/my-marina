using MyMarina.Application.Abstractions;
using MyMarina.Application.Customers;
using MyMarina.Domain.Entities;
using MyMarina.Domain.ValueObjects;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Customers;

public class CreateCustomerAccountCommandHandler(
    AppDbContext db,
    ITenantContext tenantContext) : ICommandHandler<CreateCustomerAccountCommand, Guid>
{
    public async Task<Guid> HandleAsync(CreateCustomerAccountCommand command, CancellationToken ct = default)
    {
        Address? billingAddress = command.BillingAddress is { } a
            ? new Address(a.Street, a.City, a.State, a.Zip, a.Country)
            : null;

        var account = new CustomerAccount
        {
            TenantId = tenantContext.TenantId,
            DisplayName = command.DisplayName,
            BillingEmail = command.BillingEmail,
            BillingPhone = command.BillingPhone,
            BillingAddress = billingAddress,
            EmergencyContactName = command.EmergencyContactName,
            EmergencyContactPhone = command.EmergencyContactPhone,
            Notes = command.Notes,
        };

        db.CustomerAccounts.Add(account);
        await db.SaveChangesAsync(ct);

        return account.Id;
    }
}
