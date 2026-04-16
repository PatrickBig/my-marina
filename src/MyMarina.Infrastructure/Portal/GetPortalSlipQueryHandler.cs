using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Portal;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Portal;

public class GetPortalSlipQueryHandler(
    AppDbContext db,
    ICustomerContext customerContext) : IQueryHandler<GetPortalSlipQuery, PortalSlipAssignmentDto?>
{
    public async Task<PortalSlipAssignmentDto?> HandleAsync(GetPortalSlipQuery query, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return await db.SlipAssignments
            .Where(sa => sa.CustomerAccountId == customerContext.CustomerAccountId
                      && sa.StartDate <= today
                      && (sa.EndDate == null || sa.EndDate >= today))
            .OrderByDescending(sa => sa.StartDate)
            .Select(sa => new PortalSlipAssignmentDto(
                sa.Id,
                sa.SlipId,
                sa.Slip.Name,
                sa.Slip.Dock != null ? sa.Slip.Dock.Name : null,
                sa.Slip.Marina.Name,
                sa.Boat.Name,
                sa.AssignmentType,
                sa.StartDate,
                sa.EndDate,
                sa.RateOverride,
                sa.Notes))
            .FirstOrDefaultAsync(ct);
    }
}
