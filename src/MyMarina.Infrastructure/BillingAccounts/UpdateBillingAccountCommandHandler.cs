using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.BillingAccounts;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.BillingAccounts;

public class UpdateBillingAccountCommandHandler(AppDbContext db)
    : ICommandHandler<UpdateBillingAccountCommand, BillingAccountDto>
{
    public async Task<BillingAccountDto> HandleAsync(UpdateBillingAccountCommand command, CancellationToken ct = default)
    {
        var account = await db.BillingAccounts
            .FirstOrDefaultAsync(a => a.Id == command.Id && a.MarinaId == command.MarinaId, ct)
            ?? throw new KeyNotFoundException($"BillingAccount {command.Id} not found.");

        if (command.DisplayName is not null) account.DisplayName = command.DisplayName;
        if (command.BillingEmail is not null) account.BillingEmail = command.BillingEmail;
        if (command.BillingPhone is not null) account.BillingPhone = command.BillingPhone;
        if (command.BillingAddressStreet is not null) account.BillingAddressStreet = command.BillingAddressStreet;
        if (command.BillingAddressCity is not null) account.BillingAddressCity = command.BillingAddressCity;
        if (command.BillingAddressState is not null) account.BillingAddressState = command.BillingAddressState;
        if (command.BillingAddressZip is not null) account.BillingAddressZip = command.BillingAddressZip;
        if (command.BillingAddressCountry is not null) account.BillingAddressCountry = command.BillingAddressCountry;
        if (command.EmergencyContactName is not null) account.EmergencyContactName = command.EmergencyContactName;
        if (command.EmergencyContactPhone is not null) account.EmergencyContactPhone = command.EmergencyContactPhone;
        if (command.Notes is not null) account.Notes = command.Notes;
        if (command.IsActive.HasValue) account.IsActive = command.IsActive.Value;

        await db.SaveChangesAsync(ct);
        return CreateBillingAccountCommandHandler.ToDto(account);
    }
}
