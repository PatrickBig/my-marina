using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Boats;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Boats;

public class UpdateBoatCommandHandler(AppDbContext db) : ICommandHandler<UpdateBoatCommand>
{
    public async Task HandleAsync(UpdateBoatCommand command, CancellationToken ct = default)
    {
        var boat = await db.Boats.FirstOrDefaultAsync(b => b.Id == command.BoatId, ct)
            ?? throw new KeyNotFoundException($"Boat {command.BoatId} not found.");

        boat.Name = command.Name;
        boat.Make = command.Make;
        boat.Model = command.Model;
        boat.Year = command.Year;
        boat.Length = command.Length;
        boat.Beam = command.Beam;
        boat.Draft = command.Draft;
        boat.BoatType = command.BoatType;
        boat.HullColor = command.HullColor;
        boat.RegistrationNumber = command.RegistrationNumber;
        boat.RegistrationState = command.RegistrationState;
        boat.InsuranceProvider = command.InsuranceProvider;
        boat.InsurancePolicyNumber = command.InsurancePolicyNumber;
        boat.InsuranceExpiresOn = command.InsuranceExpiresOn;
        boat.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
    }
}
