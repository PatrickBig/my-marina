namespace MyMarina.Application.Marinas;

public enum HealthStatus
{
    Healthy = 0,
    Warning = 1,
    Alert = 2
}

public sealed class HealthStatusCalculator
{
    /// <summary>
    /// Calculate marina health status based on configured targets and actual metrics.
    /// </summary>
    public static HealthStatus CalculateStatus(
        HealthTargetsDto targets,
        decimal occupancyRate,
        bool hasOverdueInvoices,
        int oldestOverdueDays)
    {
        var status = HealthStatus.Healthy;

        // Check occupancy against target
        if (targets.OccupancyRateTarget.HasValue)
        {
            var target = targets.OccupancyRateTarget.Value;
            if (occupancyRate < (target * 0.5m))
            {
                // Below 50% of target = alert
                status = HealthStatus.Alert;
            }
            else if (occupancyRate < (target * 0.8m))
            {
                // Below 80% of target = warning
                status = HealthStatus.Warning;
            }
        }

        // Check overdue AR threshold
        if (hasOverdueInvoices)
        {
            var threshold = targets.OverdueARThresholdDays ?? 30;

            if (oldestOverdueDays > (threshold * 2))
            {
                // More than 2x the threshold = alert
                status = HealthStatus.Alert;
            }
            else if (oldestOverdueDays > threshold)
            {
                // More than the threshold = warning (unless already alert)
                if (status != HealthStatus.Alert)
                {
                    status = HealthStatus.Warning;
                }
            }
        }

        return status;
    }
}
