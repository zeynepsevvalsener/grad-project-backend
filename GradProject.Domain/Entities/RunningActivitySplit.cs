namespace GradProject.Domain.Entities;

/// <summary>
/// Per-segment metrics (e.g. Strava <c>splits_metric</c>) for pace trends and split-level analytics.
/// Does not replace <see cref="RunningActivity"/> aggregates; used for drill-down and derived insights.
/// </summary>
public class RunningActivitySplit
{
    public int Id { get; set; }

    public int RunningActivityId { get; set; }

    /// <summary>Zero-based order along the activity (typically 1 km splits).</summary>
    public int Ordinal { get; set; }

    public double DistanceMeters { get; set; }

    public int MovingTimeSeconds { get; set; }

    public int ElapsedTimeSeconds { get; set; }

    /// <summary>Net elevation change for the segment (meters).</summary>
    public double ElevationDifferenceMeters { get; set; }

    /// <summary>Average speed in m/s (source API native units).</summary>
    public double AverageSpeedMetersPerSecond { get; set; }

    /// <summary>Average pace in seconds per kilometer; derived for querying/trends.</summary>
    public double PaceSecondsPerKm { get; set; }

    public RunningActivity RunningActivity { get; set; } = null!;
}
