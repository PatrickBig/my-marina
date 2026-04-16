using MyMarina.Application.Abstractions;
using MyMarina.Application.Portal;
using MyMarina.Domain.Entities;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Portal;

public class SubmitMaintenanceRequestCommandHandler(
    AppDbContext db,
    ICustomerContext customerContext,
    ITenantContext tenantContext) : ICommandHandler<SubmitMaintenanceRequestCommand, Guid>
{
    public async Task<Guid> HandleAsync(SubmitMaintenanceRequestCommand command, CancellationToken ct = default)
    {
        var request = new MaintenanceRequest
        {
            Id = Guid.CreateVersion7(),
            TenantId = tenantContext.TenantId,
            CustomerAccountId = customerContext.CustomerAccountId,
            Title = command.Title,
            Description = command.Description,
            Priority = command.Priority,
            SlipId = command.SlipId,
            BoatId = command.BoatId,
        };

        db.MaintenanceRequests.Add(request);
        await db.SaveChangesAsync(ct);
        return request.Id;
    }
}
