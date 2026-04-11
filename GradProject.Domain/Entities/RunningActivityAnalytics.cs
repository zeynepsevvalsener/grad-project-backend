namespace GradProject.Domain.Entities;

/// <summary>
/// One row per run: derived performance metrics and compact elevation/insight payloads for analytics APIs.
/// Gamification (challenge distance, territory <see cref="RunningActivity"/>) continues to use the parent activity.
/// </summary>
public class RunningActivityAnalytics
{
    public int RunningActivityId { get; set; }

    /// <summary>Standard deviation of split paces (sec/km); null if not enough splits.</summary>
    public double? PaceVariabilitySecondsPerKm { get; set; }

    public double? FirstHalfPaceSecondsPerKm { get; set; }

    public double? SecondHalfPaceSecondsPerKm { get; set; }

    /// <summary>True when second half average pace is faster (lower sec/km) than first half.</summary>
    public bool? IsNegativeSplit { get; set; }

    /// <summary>moving_time / elapsed_time when elapsed &gt; 0.</summary>
    public double? MovingTimeRatio { get; set; }

    /// <summary>Compact elevation summary (JSON): min/max elevation, gain/loss estimates.</summary>
    public string? ElevationSummaryJson { get; set; }

    /// <summary>Human-readable or machine-readable insights (JSON array of strings or objects).</summary>
    public string? PerformanceInsightsJson { get; set; }

    public DateTime? ComputedAt { get; set; }

    public RunningActivity RunningActivity { get; set; } = null!;
}
