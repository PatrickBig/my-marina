using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Vessels;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Vessels;

public class UpdateVesselCommandHandler(AppDbContext db)
    : ICommandHandler<UpdateVesselCommand>
{
    public async Task HandleAsync(UpdateVesselCommand command, CancellationToken ct = default)
    {
        var vessel = await db.Vessels
            .FirstOrDefaultAsync(v => v.Id == command.Id && v.OwnerUserId == command.OwnerId, ct)
            ?? throw new KeyNotFoundException("Vessel not found.");

        if (vessel.IsArchived)
            throw new InvalidOperationException("Cannot edit an archived vessel.");

        if (command.Name is not null)               vessel.Name               = command.Name;
        if (command.Make is not null)               vessel.Make               = command.Make;
        if (command.Model is not null)              vessel.Model              = command.Model;
        if (command.Year.HasValue)                  vessel.Year               = command.Year;
        if (command.Length.HasValue)                vessel.Length             = command.Length.Value;
        if (command.Beam.HasValue)                  vessel.Beam               = command.Beam.Value;
        if (command.Draft.HasValue)                 vessel.Draft              = command.Draft.Value;
        if (command.BoatType.HasValue)              vessel.BoatType           = command.BoatType.Value;
        if (command.HullColor is not null)          vessel.HullColor          = command.HullColor;
        if (command.RegistrationNumber is not null) vessel.RegistrationNumber = command.RegistrationNumber;
        if (command.RegistrationState is not null)  vessel.RegistrationState  = command.RegistrationState;

        await db.SaveChangesAsync(ct);
    }
}
