using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Marinas;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Marinas;

public class UpdateMarinaCommandHandler(AppDbContext db)
    : ICommandHandler<UpdateMarinaCommand, MarinaDto>
{
    public async Task<MarinaDto> HandleAsync(UpdateMarinaCommand command, CancellationToken ct = default)
    {
        var marina = await db.Marinas
            .FirstOrDefaultAsync(m => m.Id == command.MarinaId, ct)
            ?? throw new KeyNotFoundException("Marina not found.");

        if (command.Name is not null) marina.Name = command.Name;
        if (command.AddressStreet is not null) marina.AddressStreet = command.AddressStreet;
        if (command.AddressCity is not null) marina.AddressCity = command.AddressCity;
        if (command.AddressState is not null) marina.AddressState = command.AddressState;
        if (command.AddressZip is not null) marina.AddressZip = command.AddressZip;
        if (command.AddressCountry is not null) marina.AddressCountry = command.AddressCountry;
        if (command.Latitude.HasValue) marina.Latitude = command.Latitude;
        if (command.Longitude.HasValue) marina.Longitude = command.Longitude;
        if (command.PhoneNumber is not null) marina.PhoneNumber = command.PhoneNumber;
        if (command.Email is not null) marina.Email = command.Email;
        if (command.Website is not null) marina.Website = command.Website;
        if (command.Description is not null) marina.Description = command.Description;
        if (command.TimeZoneId is not null) marina.TimeZoneId = command.TimeZoneId;

        await db.SaveChangesAsync(ct);
        return MarinaMappers.ToMarinaDto(marina);
    }
}
