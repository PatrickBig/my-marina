using MyMarina.Application.Abstractions;
using MyMarina.Application.Boats;
using MyMarina.Domain.Entities;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Boats;

public class CreateBoatCommandHandler(
    AppDbContext db,
    ITenantContext tenantContext) : ICommandHandler<CreateBoatCommand, Guid>
{
    public async Task<Guid> HandleAsync(CreateBoatCommand command, CancellationToken ct = default)
    {
        var boat = new Boat
        {
            TenantId = tenantContext.TenantId,
            CustomerAccountId = command.CustomerAccountId,
            Name = command.Name,
            Make = command.Make,
            Model = command.Model,
            Year = command.Year,
            Length = command.Length,
            Beam = command.Beam,
            Draft = command.Draft,
            BoatType = command.BoatType,
            HullColor = command.HullColor,
            RegistrationNumber = command.RegistrationNumber,
            RegistrationState = command.RegistrationState,
            InsuranceProvider = command.InsuranceProvider,
            InsurancePolicyNumber = command.InsurancePolicyNumber,
            InsuranceExpiresOn = command.InsuranceExpiresOn,
        };

        db.Boats.Add(boat);
        await db.SaveChangesAsync(ct);

        return boat.Id;
    }
}
