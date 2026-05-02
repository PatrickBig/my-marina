using MyMarina.Application.Abstractions;
using MyMarina.Application.Vessels;
using MyMarina.Domain.Entities;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Vessels;

public class CreateVesselCommandHandler(AppDbContext db)
    : ICommandHandler<CreateVesselCommand, VesselDto>
{
    public async Task<VesselDto> HandleAsync(CreateVesselCommand command, CancellationToken ct = default)
    {
        var vessel = new Vessel
        {
            OwnerUserId        = command.OwnerId,
            Name               = command.Name,
            Make               = command.Make,
            Model              = command.Model,
            Year               = command.Year,
            Length             = command.Length,
            Beam               = command.Beam,
            Draft              = command.Draft,
            BoatType           = command.BoatType,
            HullColor          = command.HullColor,
            RegistrationNumber = command.RegistrationNumber,
            RegistrationState  = command.RegistrationState,
        };

        db.Vessels.Add(vessel);
        await db.SaveChangesAsync(ct);

        return ToDto(vessel);
    }

    internal static VesselDto ToDto(Vessel v) => new(
        Id: v.Id,
        Name: v.Name,
        Make: v.Make,
        Model: v.Model,
        Year: v.Year,
        Length: v.Length,
        Beam: v.Beam,
        Draft: v.Draft,
        BoatType: v.BoatType.ToString(),
        HullColor: v.HullColor,
        RegistrationNumber: v.RegistrationNumber,
        RegistrationState: v.RegistrationState,
        IsArchived: v.IsArchived,
        CreatedAt: v.CreatedAt
    );
}
