using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Marinas;
using MyMarina.Domain.ValueObjects;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Marinas;

public class UpdateMarinaCommandHandler(AppDbContext db) : ICommandHandler<UpdateMarinaCommand>
{
    public async Task HandleAsync(UpdateMarinaCommand command, CancellationToken ct = default)
    {
        var marina = await db.Marinas.FirstOrDefaultAsync(m => m.Id == command.MarinaId, ct)
            ?? throw new KeyNotFoundException($"Marina {command.MarinaId} not found.");

        marina.Name = command.Name;
        marina.Address = new Address(
            command.Address.Street,
            command.Address.City,
            command.Address.State,
            command.Address.Zip,
            command.Address.Country);
        marina.PhoneNumber = command.PhoneNumber;
        marina.Email = command.Email;
        marina.TimeZoneId = command.TimeZoneId;
        marina.Website = command.Website;
        marina.Description = command.Description;
        marina.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
    }
}
