namespace GradProject.Application.DTOs.Common;

/// <summary>Per-km (or per-segment) split returned with run detail APIs.</summary>
public class RunActivitySplitDto
{
    public int Ordinal { get; set; }
    public double DistanceMeters { get; set; }
    public int MovingTimeSeconds { get; set; }
    public int ElapsedTimeSeconds { get; set; }
    public double ElevationDifferenceMeters { get; set; }
    public double AverageSpeedMetersPerSecond { get; set; }
    public double PaceSecondsPerKm { get; set; }
}

/// <summary>Derived analytics for dashboards; does not affect challenge/leaderboard distance rules.</summary>
public class RunActivityAnalyticsDto
{
    public double? PaceVariabilitySecondsPerKm { get; set; }
    public double? FirstHalfPaceSecondsPerKm { get; set; }
    public double? SecondHalfPaceSecondsPerKm { get; set; }
    public bool? IsNegativeSplit { get; set; }
    public double? MovingTimeRatio { get; set; }
    public string? ElevationSummaryJson { get; set; }
    public string? PerformanceInsightsJson { get; set; }
    public DateTime? ComputedAt { get; set; }
}
