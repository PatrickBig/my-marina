using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Customers;
using MyMarina.Domain.ValueObjects;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Customers;

public class UpdateCustomerAccountCommandHandler(AppDbContext db) : ICommandHandler<UpdateCustomerAccountCommand>
{
    public async Task HandleAsync(UpdateCustomerAccountCommand command, CancellationToken ct = default)
    {
        var account = await db.CustomerAccounts.FirstOrDefaultAsync(a => a.Id == command.CustomerAccountId, ct)
            ?? throw new KeyNotFoundException($"CustomerAccount {command.CustomerAccountId} not found.");

        account.DisplayName = command.DisplayName;
        account.BillingEmail = command.BillingEmail;
        account.BillingPhone = command.BillingPhone;
        account.BillingAddress = command.BillingAddress is { } a
            ? new Address(a.Street, a.City, a.State, a.Zip, a.Country)
            : null;
        account.EmergencyContactName = command.EmergencyContactName;
        account.EmergencyContactPhone = command.EmergencyContactPhone;
        account.Notes = command.Notes;
        account.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
    }
}
