using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Customers;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Customers;

public class DeactivateCustomerAccountCommandHandler(AppDbContext db) : ICommandHandler<DeactivateCustomerAccountCommand>
{
    public async Task HandleAsync(DeactivateCustomerAccountCommand command, CancellationToken ct = default)
    {
        var account = await db.CustomerAccounts.FirstOrDefaultAsync(a => a.Id == command.CustomerAccountId, ct)
            ?? throw new KeyNotFoundException($"CustomerAccount {command.CustomerAccountId} not found.");

        account.IsActive = false;
        account.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
    }
}
