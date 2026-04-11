using System.Text.Json;
using GradProject.Domain.Entities;

namespace GradProject.Infrastructure.Services.Running;

/// <summary>
/// Derives compact analytics from stored splits and activity totals. Safe for gamification paths that only need <see cref="RunningActivity"/> aggregates.
/// </summary>
public static class RunningActivityAnalyticsBuilder
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static RunningActivityAnalytics Build(RunningActivity activity)
    {
        var splits = activity.Splits.OrderBy(s => s.Ordinal).ToList();
        var now = DateTime.UtcNow;

        double? paceVariability = null;
        double? firstHalf = null;
        double? secondHalf = null;
        bool? negativeSplit = null;

        var usablePaces = splits
            .Where(s => s.DistanceMeters > 1 && s.PaceSecondsPerKm > 0)
            .Select(s => s.PaceSecondsPerKm)
            .ToList();

        if (usablePaces.Count >= 2)
        {
            var mean = usablePaces.Average();
            paceVariability = Math.Sqrt(usablePaces.Sum(p => (p - mean) * (p - mean)) / usablePaces.Count);
        }

        if (splits.Count >= 2)
        {
            var mid = splits.Count / 2;
            var firstSegs = splits.Take(mid).ToList();
            var secondSegs = splits.Skip(mid).ToList();
            firstHalf = WeightedMeanPace(firstSegs);
            secondHalf = WeightedMeanPace(secondSegs);
            if (firstHalf is > 0 and { } f && secondHalf is { } sec)
                negativeSplit = sec < f;
        }

        double? movingRatio = null;
        if (activity.ElapsedTimeSeconds > 0)
            movingRatio = activity.MovingTimeSeconds / (double)activity.ElapsedTimeSeconds;

        var elevSummary = new
        {
            minM = activity.ElevLowMeters,
            maxM = activity.ElevHighMeters,
            reportedGainM = activity.TotalElevationGain
        };
        var elevationJson = JsonSerializer.Serialize(elevSummary, JsonOpts);

        var insights = new List<string>();
        if (negativeSplit == true)
            insights.Add("negative_split");
        if (paceVariability is > 0 and < 15)
            insights.Add("steady_pace");
        else if (paceVariability is >= 15)
            insights.Add("variable_pace");
        if (movingRatio is < 0.85)
            insights.Add("many_stops");

        var insightsJson = insights.Count > 0 ? JsonSerializer.Serialize(insights, JsonOpts) : null;

        return new RunningActivityAnalytics
        {
            PaceVariabilitySecondsPerKm = paceVariability,
            FirstHalfPaceSecondsPerKm = firstHalf,
            SecondHalfPaceSecondsPerKm = secondHalf,
            IsNegativeSplit = negativeSplit,
            MovingTimeRatio = movingRatio,
            ElevationSummaryJson = elevationJson,
            PerformanceInsightsJson = insightsJson,
            ComputedAt = now
        };
    }

    private static double? WeightedMeanPace(IReadOnlyList<RunningActivitySplit> segments)
    {
        var dist = segments.Sum(s => s.DistanceMeters);
        if (dist < 1)
            return null;
        var num = segments.Sum(s => s.PaceSecondsPerKm * s.DistanceMeters);
        return num / dist;
    }
}
