using Microsoft.AspNetCore.Identity;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Maintenance;
using MyMarina.Domain.Entities;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Identity;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Maintenance;

public class CreateWorkOrderCommandHandler(
    AppDbContext db,
    UserManager<ApplicationUser> userManager)
    : ICommandHandler<CreateWorkOrderCommand, WorkOrderDto>
{
    public async Task<WorkOrderDto> HandleAsync(
        CreateWorkOrderCommand cmd, CancellationToken ct = default)
    {
        var workOrder = new WorkOrder
        {
            MarinaId             = cmd.MarinaId,
            MaintenanceRequestId = cmd.MaintenanceRequestId,
            Title                = cmd.Title,
            Description          = cmd.Description,
            AssignedToUserId     = cmd.AssignedToUserId,
            Priority             = Enum.Parse<MaintenancePriority>(cmd.Priority),
            ScheduledDate        = cmd.ScheduledDate,
        };

        db.WorkOrders.Add(workOrder);
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
