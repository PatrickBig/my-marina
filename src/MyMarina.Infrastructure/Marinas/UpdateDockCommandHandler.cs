using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Marinas;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Marinas;

public class UpdateDockCommandHandler(AppDbContext db)
    : ICommandHandler<UpdateDockCommand, DockDto>
{
    public async Task<DockDto> HandleAsync(UpdateDockCommand command, CancellationToken ct = default)
    {
        var dock = await db.Docks
            .FirstOrDefaultAsync(d => d.Id == command.DockId && d.MarinaId == command.MarinaId, ct)
            ?? throw new KeyNotFoundException("Dock not found.");

        if (command.Name is not null) dock.Name = command.Name;
        if (command.Description is not null) dock.Description = command.Description;
        if (command.SortOrder.HasValue) dock.SortOrder = command.SortOrder.Value;

        await db.SaveChangesAsync(ct);
        return MarinaMappers.ToDockDto(dock);
    }
}
