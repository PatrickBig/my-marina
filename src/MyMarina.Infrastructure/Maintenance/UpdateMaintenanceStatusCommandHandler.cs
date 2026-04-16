using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Maintenance;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Maintenance;

public class UpdateMaintenanceStatusCommandHandler(AppDbContext db)
    : ICommandHandler<UpdateMaintenanceStatusCommand>
{
    public async Task HandleAsync(UpdateMaintenanceStatusCommand command, CancellationToken ct = default)
    {
        var request = await db.MaintenanceRequests
            .FirstOrDefaultAsync(r => r.Id == command.MaintenanceRequestId, ct)
            ?? throw new KeyNotFoundException($"Maintenance request {command.MaintenanceRequestId} not found.");

        request.Status = command.Status;

        if (command.Status is MaintenanceStatus.Completed or MaintenanceStatus.Declined)
            request.ResolvedAt ??= DateTimeOffset.UtcNow;
        else
            request.ResolvedAt = null;

        await db.SaveChangesAsync(ct);
    }
}
