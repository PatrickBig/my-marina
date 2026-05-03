using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Maintenance;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Identity;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Maintenance;

public class UpdateWorkOrderCommandHandler(
    AppDbContext db,
    UserManager<ApplicationUser> userManager)
    : ICommandHandler<UpdateWorkOrderCommand, WorkOrderDto>
{
    public async Task<WorkOrderDto> HandleAsync(
        UpdateWorkOrderCommand cmd, CancellationToken ct = default)
    {
        var workOrder = await db.WorkOrders
            .FirstOrDefaultAsync(w => w.MarinaId == cmd.MarinaId && w.Id == cmd.WorkOrderId, ct)
            ?? throw new KeyNotFoundException("Work order not found.");

        var newStatus = Enum.Parse<WorkOrderStatus>(cmd.Status);

        workOrder.Title            = cmd.Title;
        workOrder.Description      = cmd.Description;
        workOrder.AssignedToUserId = cmd.AssignedToUserId;
        workOrder.Status           = newStatus;
        workOrder.Priority         = Enum.Parse<MaintenancePriority>(cmd.Priority);
        workOrder.ScheduledDate    = cmd.ScheduledDate;
        workOrder.Notes            = cmd.Notes;

        if (newStatus == WorkOrderStatus.Completed)
            workOrder.CompletedAt ??= DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);

        string? assignedName = null;
        if (cmd.AssignedToUserId.HasValue)
        {
            var staff = await userManager.FindByIdAsync(cmd.AssignedToUserId.Value.ToString());
            assignedName = staff is null ? null : $"{staff.FirstName} {staff.LastName}".Trim();
        }

        return MaintenanceHelper.ToDto(workOrder, assignedName);
    }
}
