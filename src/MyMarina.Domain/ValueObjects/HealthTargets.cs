namespace MyMarina.Domain.ValueObjects;

/// <summary>
/// Extensible health targets configuration for a marina.
/// Stored as JSON in the database; new targets can be added without schema changes.
/// </summary>
public sealed record HealthTargets(
    decimal? OccupancyRateTarget = null,
    int? OverdueARThresholdDays = null,
    decimal? TargetMonthlyRevenue = null)
{
    /// <summary>
    /// Create default health targets (70% occupancy, 30 day AR threshold).
    /// </summary>
    public static HealthTargets CreateDefaults()
    {
        return new HealthTargets(
            OccupancyRateTarget: 70m,
            OverdueARThresholdDays: 30);
    }
}
