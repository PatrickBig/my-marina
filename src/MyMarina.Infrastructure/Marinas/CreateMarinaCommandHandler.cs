using MyMarina.Application.Abstractions;
using MyMarina.Application.Marinas;
using MyMarina.Domain.Entities;
using MyMarina.Domain.ValueObjects;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Marinas;

public class CreateMarinaCommandHandler(
    AppDbContext db,
    ITenantContext tenantContext) : ICommandHandler<CreateMarinaCommand, Guid>
{
    public async Task<Guid> HandleAsync(CreateMarinaCommand command, CancellationToken ct = default)
    {
        var marina = new Marina
        {
            TenantId = tenantContext.TenantId,
            Name = command.Name,
            Address = new Address(
                command.Address.Street,
                command.Address.City,
                command.Address.State,
                command.Address.Zip,
                command.Address.Country),
            PhoneNumber = command.PhoneNumber,
            Email = command.Email,
            TimeZoneId = command.TimeZoneId,
            Website = command.Website,
            Description = command.Description,
        };

        db.Marinas.Add(marina);
        await db.SaveChangesAsync(ct);

        return marina.Id;
    }
}
