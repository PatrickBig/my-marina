using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Marinas;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Marinas;

public class GetSlipsQueryHandler(AppDbContext db)
    : IQueryHandler<GetSlipsQuery, IReadOnlyList<SlipDto>>
{
    public async Task<IReadOnlyList<SlipDto>> HandleAsync(GetSlipsQuery query, CancellationToken ct = default)
    {
        var q = db.Slips.Where(s => s.MarinaId == query.MarinaId);
        if (query.DockId.HasValue)
            q = q.Where(s => s.DockId == query.DockId);

        var slips = await q.OrderBy(s => s.Name).ToListAsync(ct);
        if (slips.Count == 0) return [];

        // Single query for all current holders — EF joins BillingAccounts automatically
        var today = DateOnly.FromDateTime(DateTime.Today);
        var slipIds = slips.Select(s => s.Id).ToList();

        var holderRows = await db.SlipAssignments
            .Where(a => slipIds.Contains(a.SlipId)
                     && a.StartDate <= today
                     && (a.EndDate == null || a.EndDate >= today))
            .Select(a => new { a.SlipId, HolderName = a.BillingAccount.DisplayName, a.AssignmentType })
            .ToListAsync(ct);

        var holders = holderRows
            .DistinctBy(a => a.SlipId)
            .ToDictionary(a => a.SlipId, a => $"{a.HolderName} · {a.AssignmentType.ToString().ToLower()}");

        return slips.Select(s => MarinaMappers.ToSlipDto(s) with
        {
            CurrentHolder = holders.GetValueOrDefault(s.Id)
        }).ToList();
    }
}
