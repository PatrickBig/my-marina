using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Maintenance;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Identity;
using MyMarina.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

namespace MyMarina.Infrastructure.Maintenance;

public class UpdateMaintenanceRequestCommandHandler(
    AppDbContext db,
    UserManager<ApplicationUser> userManager)
    : ICommandHandler<UpdateMaintenanceRequestCommand, MaintenanceRequestDto>
{
    public async Task<MaintenanceRequestDto> HandleAsync(
        UpdateMaintenanceRequestCommand cmd, CancellationToken ct = default)
    {
        var request = await db.MaintenanceRequests
            .Include(r => r.WorkOrder)
            .FirstOrDefaultAsync(r => r.MarinaId == cmd.MarinaId && r.Id == cmd.RequestId, ct)
            ?? throw new KeyNotFoundException("Maintenance request not found.");

        var newStatus = Enum.Parse<MaintenanceRequestStatus>(cmd.Status);
        request.Status   = newStatus;
        request.Priority = Enum.Parse<MaintenancePriority>(cmd.Priority);

        if (newStatus is MaintenanceRequestStatus.Completed or MaintenanceRequestStatus.Declined)
            request.ResolvedAt ??= DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);

        var appUser = await userManager.FindByIdAsync(request.BoaterUserId.ToString());
        var name    = appUser is null ? "" : $"{appUser.FirstName} {appUser.LastName}".Trim();

        return MaintenanceHelper.ToDto(request, name);
    }
}
