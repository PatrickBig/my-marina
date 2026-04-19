namespace MyMarina.Application.Marinas;

public enum HealthStatus
{
    Healthy = 0,
    Warning = 1,
    Alert = 2
}

public sealed class HealthStatusCalculator
{
    public static HealthStatus CalculateStatus(
        HealthTargetsDto targets,
        decimal occupancyRate,
        bool hasOverdueInvoices,
        int oldestOverdueDays)
    {
        var status = HealthStatus.Healthy;

        if (targets.OccupancyAlertThreshold.HasValue && occupancyRate < targets.OccupancyAlertThreshold.Value)
            status = HealthStatus.Alert;
        else if (targets.OccupancyWarningThreshold.HasValue && occupancyRate < targets.OccupancyWarningThreshold.Value)
            status = HealthStatus.Warning;

        if (hasOverdueInvoices)
        {
            if (targets.OverdueAlertDays.HasValue && oldestOverdueDays > targets.OverdueAlertDays.Value)
                status = HealthStatus.Alert;
            else if (targets.OverdueWarningDays.HasValue && oldestOverdueDays > targets.OverdueWarningDays.Value
                     && status != HealthStatus.Alert)
                status = HealthStatus.Warning;
        }

        return status;
    }
}
