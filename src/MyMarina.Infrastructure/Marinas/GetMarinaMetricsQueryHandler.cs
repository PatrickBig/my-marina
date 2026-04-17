using Microsoft.EntityFrameworkCore;
using MyMarina.Application.Abstractions;
using MyMarina.Application.Marinas;
using MyMarina.Domain.Enums;
using MyMarina.Infrastructure.Persistence;

namespace MyMarina.Infrastructure.Marinas;

public class GetMarinaMetricsQueryHandler(AppDbContext db) : IQueryHandler<GetMarinaMetricsQuery, MarinaMetricsDto?>
{
    public async Task<MarinaMetricsDto?> HandleAsync(GetMarinaMetricsQuery query, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        // Check marina exists
        var marina = await db.Marinas.FirstOrDefaultAsync(m => m.Id == query.MarinaId, ct);
        if (marina is null) return null;

        // Run metric queries sequentially (EF Core DbContext doesn't support concurrent operations)
        var totalSlips = await db.Slips
            .Where(s => s.MarinaId == query.MarinaId && s.Status != SlipStatus.Inactive)
            .CountAsync(ct);

        var occupiedSlips = await db.SlipAssignments
            .Where(a => a.Slip.MarinaId == query.MarinaId &&
                        (a.EndDate == null || a.EndDate >= today))
            .Select(a => a.SlipId)
            .Distinct()
            .CountAsync(ct);

        var outstandingAR = await db.Invoices
            .Where(i => i.MarinaId == query.MarinaId &&
                        (i.Status == InvoiceStatus.Sent ||
                         i.Status == InvoiceStatus.PartiallyPaid ||
                         i.Status == InvoiceStatus.Overdue))
            .SumAsync(i => i.TotalAmount - i.AmountPaid, ct);

        var oldestOverdueDate = await db.Invoices
            .Where(i => i.MarinaId == query.MarinaId && i.Status == InvoiceStatus.Overdue)
            .MinAsync(i => (DateOnly?)i.DueDate, ct);

        var activeCustomerCount = await db.SlipAssignments
            .Where(a => a.Slip.MarinaId == query.MarinaId &&
                        (a.EndDate == null || a.EndDate >= today) &&
                        a.CustomerAccount.IsActive)
            .Select(a => a.CustomerAccountId)
            .Distinct()
            .CountAsync(ct);

        // Calculate derived metrics
        var occupancyRate = totalSlips == 0 ? 0m : (occupiedSlips / (decimal)totalSlips) * 100;
        var oldestOverdueDays = oldestOverdueDate.HasValue
            ? (int)(today.ToDateTime(TimeOnly.MinValue) - oldestOverdueDate.Value.ToDateTime(TimeOnly.MinValue)).TotalDays
            : 0;

        // Load health targets and calculate status
        var healthTargets = marina.HealthTargets;
        var healthStatus = HealthStatusCalculator.CalculateStatus(
            new HealthTargetsDto(
                healthTargets.OccupancyRateTarget,
                healthTargets.OverdueARThresholdDays,
                healthTargets.TargetMonthlyRevenue),
            occupancyRate,
            oldestOverdueDays > 0,
            oldestOverdueDays);

        return new MarinaMetricsDto(
            TotalSlips: totalSlips,
            OccupiedSlips: occupiedSlips,
            OccupancyRate: occupancyRate,
            OutstandingAR: outstandingAR,
            OldestOverdueDays: oldestOverdueDays,
            ActiveCustomerCount: activeCustomerCount,
            HealthStatus: healthStatus);
    }
}
