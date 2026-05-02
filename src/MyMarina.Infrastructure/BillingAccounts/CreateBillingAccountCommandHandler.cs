using MyMarina.Application.Abstractions;
using MyMarina.Application.BillingAccounts;
using MyMarina.Domain.Entities;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.BillingAccounts;

public class CreateBillingAccountCommandHandler(AppDbContext db)
    : ICommandHandler<CreateBillingAccountCommand, BillingAccountDto>
{
    public async Task<BillingAccountDto> HandleAsync(CreateBillingAccountCommand command, CancellationToken ct = default)
    {
        var account = new BillingAccount
        {
            MarinaId                = command.MarinaId,
            DisplayName             = command.DisplayName,
            BillingEmail            = command.BillingEmail,
            BillingPhone            = command.BillingPhone,
            BillingAddressStreet    = command.BillingAddressStreet,
            BillingAddressCity      = command.BillingAddressCity,
            BillingAddressState     = command.BillingAddressState,
            BillingAddressZip       = command.BillingAddressZip,
            BillingAddressCountry   = command.BillingAddressCountry,
            EmergencyContactName    = command.EmergencyContactName,
            EmergencyContactPhone   = command.EmergencyContactPhone,
            Notes                   = command.Notes,
        };

        db.BillingAccounts.Add(account);
        await db.SaveChangesAsync(ct);

        return ToDto(account);
    }

    internal static BillingAccountDto ToDto(BillingAccount a) => new(
        Id:                     a.Id,
        MarinaId:               a.MarinaId,
        DisplayName:            a.DisplayName,
        BillingEmail:           a.BillingEmail,
        BillingPhone:           a.BillingPhone,
        BillingAddressStreet:   a.BillingAddressStreet,
        BillingAddressCity:     a.BillingAddressCity,
        BillingAddressState:    a.BillingAddressState,
        BillingAddressZip:      a.BillingAddressZip,
        BillingAddressCountry:  a.BillingAddressCountry,
        EmergencyContactName:   a.EmergencyContactName,
        EmergencyContactPhone:  a.EmergencyContactPhone,
        IsActive:               a.IsActive,
        CreatedAt:              a.CreatedAt
    );
}
