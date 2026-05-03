using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Maintenance;
using MyMarina.Domain.Entities;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Identity;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Maintenance;

public class SubmitMaintenanceRequestCommandHandler(
    AppDbContext db,
    IUserContext user,
    UserManager<ApplicationUser> userManager)
    : ICommandHandler<SubmitMaintenanceRequestCommand, MaintenanceRequestDto>
{
    public async Task<MaintenanceRequestDto> HandleAsync(
        SubmitMaintenanceRequestCommand cmd, CancellationToken ct = default)
    {
        var request = new MaintenanceRequest
        {
            MarinaId         = cmd.MarinaId,
            BoaterUserId     = user.UserId!.Value,
            BillingAccountId = cmd.BillingAccountId,
            VesselId         = cmd.VesselId,
            SlipId           = cmd.SlipId,
            ReservationId    = cmd.ReservationId,
            Title            = cmd.Title,
            Description      = cmd.Description,
            Priority         = Enum.Parse<MaintenancePriority>(cmd.Priority),
        };

        db.MaintenanceRequests.Add(request);
        await db.SaveChangesAsync(ct);

        var appUser = await userManager.FindByIdAsync(user.UserId!.Value.ToString());
        var name    = appUser is null ? "" : $"{appUser.FirstName} {appUser.LastName}".Trim();

        return MaintenanceHelper.ToDto(request, name);
    }
}
