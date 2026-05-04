using MyMarina.Application.Abstractions;
using MyMarina.Application.Marinas;
using MyMarina.Domain.Entities;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Marinas;

public class CreateDockCommandHandler(AppDbContext db)
    : ICommandHandler<CreateDockCommand, DockDto>
{
    public async Task<DockDto> HandleAsync(CreateDockCommand command, CancellationToken ct = default)
    {
        var dock = new Dock
        {
            MarinaId = command.MarinaId,
            Name = command.Name,
            Description = command.Description,
            SortOrder = command.SortOrder,
        };
        db.Docks.Add(dock);
        await db.SaveChangesAsync(ct);
        return MarinaMappers.ToDockDto(dock);
    }
}
