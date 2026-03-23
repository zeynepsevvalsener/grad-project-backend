using GradProject.Application.DTOs.Running;
using GradProject.Application.Interfaces.Running;
using GradProject.Application.Utilities;
using GradProject.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GradProject.Infrastructure.Services.Running
{
    public class RunningAnalyticsService : IRunningAnalyticsService
    {
        private readonly AppDbContext _db;

        public RunningAnalyticsService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<RunningAnalyticsResponseDto> GetAnalyticsAsync(int userId, CancellationToken ct = default)
        {
            var paceTrend = await GetPaceTrendAsync(userId, ct);
            var heartRateTrend = await GetHeartRateTrendAsync(userId, ct);

            return new RunningAnalyticsResponseDto
            {
                PaceTrend = paceTrend,
                HeartRateTrend = heartRateTrend,
                CalculatedAt = DateTime.UtcNow
            };
        }

        public async Task<PaceTrendResponseDto> GetPaceTrendAsync(int userId, CancellationToken ct = default)
        {
            // Get all RunningActivities for the user (no date filter - use all available data)
            var activities = await _db.RunningActivities
                .AsNoTracking()
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.StartTime)
                .ToListAsync(ct);

            // Calculate average pace from all activities (or last 7 days if we have many)
            var validActivities = activities
                .Where(a => a.DistanceMeters > 0 && a.MovingTimeSeconds > 0)
                .ToList();

            var weeklyPaces = new List<double>();
            var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);
            
            // Use last 7 days for weekly average, or all if less than 7 days of data
            var recentActivities = validActivities
                .Where(a => a.StartTime >= sevenDaysAgo)
                .ToList();

            var activitiesForWeeklyAvg = recentActivities.Count > 0 ? recentActivities : validActivities;

            var secondsPerKmList = activitiesForWeeklyAvg
                .Select(a => (a.MovingTimeSeconds / a.DistanceMeters) * 1000.0)
                .Where(s => s > 0)
                .ToList();

            var weeklyAveragePace = secondsPerKmList.Count > 0
                ? RunningPaceCalculator.FromSecondsPerKm(secondsPerKmList.Average())
                : 0.0;

            // Get last 5 runs comparison
            var last5Runs = new List<PaceComparisonDto>();
            var last5ValidActivities = validActivities
                .Take(5)
                .ToList();

            double? previousPace = null;
            double? firstPace = null;
            double? lastPace = null;

            for (int i = 0; i < last5ValidActivities.Count; i++)
            {
                var activity = last5ValidActivities[i];
                var pace = RunningPaceCalculator.FromDistanceAndTime(activity.DistanceMeters, activity.MovingTimeSeconds);

                if (i == 0)
                    firstPace = pace;
                if (i == last5ValidActivities.Count - 1)
                    lastPace = pace;

                double? delta = previousPace.HasValue
                    ? Math.Round(pace - previousPace.Value, 2, MidpointRounding.AwayFromZero)
                    : (double?)null;
                double? improvementPercentage = previousPace.HasValue && previousPace.Value > 0
                    ? Math.Round(RunningPaceCalculator.ImprovementPercent(previousPace.Value, pace), 2, MidpointRounding.AwayFromZero)
                    : (double?)null;

                last5Runs.Add(new PaceComparisonDto
                {
                    ActivityId = activity.Id,
                    ActivityDate = activity.StartTime,
                    Pace = pace,
                    DeltaFromPrevious = delta,
                    ImprovementPercentage = improvementPercentage
                });

                previousPace = pace;
            }

            // Calculate overall improvement percentage (first vs last)
            double? overallImprovement = null;
            if (firstPace.HasValue && lastPace.HasValue && firstPace.Value > 0)
            {
                overallImprovement = Math.Round(RunningPaceCalculator.ImprovementPercent(firstPace.Value, lastPace.Value), 2, MidpointRounding.AwayFromZero);
            }

            return new PaceTrendResponseDto
            {
                WeeklyAveragePace = weeklyAveragePace,
                Last5RunsComparison = last5Runs,
                OverallImprovementPercentage = overallImprovement
            };
        }

        public async Task<HeartRateTrendResponseDto> GetHeartRateTrendAsync(int userId, CancellationToken ct = default)
        {
            // Get all RunningActivities with HR data (no date filter - use all available data)
            var activities = await _db.RunningActivities
                .AsNoTracking()
                .Where(r => r.UserId == userId && r.AverageHeartRate.HasValue)
                .OrderByDescending(r => r.StartTime)
                .ToListAsync(ct);

            // Calculate weekly average HR (use last 7 days if available, otherwise all)
            var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);
            var recentActivities = activities
                .Where(r => r.StartTime >= sevenDaysAgo)
                .ToList();

            var activitiesForWeeklyAvg = recentActivities.Count > 0 ? recentActivities : activities;

            var heartRates = activitiesForWeeklyAvg
                .Where(r => r.AverageHeartRate.HasValue)
                .Select(r => r.AverageHeartRate!.Value)
                .ToList();

            var weeklyAverageHeartRate = heartRates.Count > 0 ? Math.Round(heartRates.Average(), 2, MidpointRounding.AwayFromZero) : (double?)null;

            // Get last 5 runs with HR data
            var last5RunsTrend = new List<HeartRateDataPointDto>();
            var last5Activities = activities.Take(5).ToList();

            foreach (var activity in last5Activities)
            {
                if (activity.AverageHeartRate.HasValue && activity.DistanceMeters > 0 && activity.MovingTimeSeconds > 0)
                {
                    var pace = RunningPaceCalculator.FromDistanceAndTime(activity.DistanceMeters, activity.MovingTimeSeconds);
                    last5RunsTrend.Add(new HeartRateDataPointDto
                    {
                        ActivityId = activity.Id,
                        ActivityDate = activity.StartTime,
                        AverageHeartRate = Math.Round(activity.AverageHeartRate.Value, 2, MidpointRounding.AwayFromZero),
                        Pace = pace
                    });
                }
            }

            // Determine trend direction
            var trendDirection = DetermineTrendDirection(last5RunsTrend);

            return new HeartRateTrendResponseDto
            {
                WeeklyAverageHeartRate = weeklyAverageHeartRate,
                Last5RunsTrend = last5RunsTrend,
                TrendDirection = trendDirection
            };
        }

        /// <summary>
        /// Determines HR trend direction using simple comparison
        /// If last 3 runs average > first 2 runs average = "Increasing"
        /// If last 3 runs average &lt; first 2 runs average = "Decreasing"
        /// Otherwise = "Stable"
        /// </summary>
        private string DetermineTrendDirection(List<HeartRateDataPointDto> dataPoints)
        {
            if (dataPoints.Count < 3)
                return "Stable";

            // Sort by date (oldest first)
            var sorted = dataPoints.OrderBy(d => d.ActivityDate).ToList();

            var firstTwo = sorted.Take(2).Select(d => d.AverageHeartRate).ToList();
            var lastThree = sorted.Skip(Math.Max(0, sorted.Count - 3)).Take(3).Select(d => d.AverageHeartRate).ToList();

            if (firstTwo.Count > 0 && lastThree.Count > 0)
            {
                var firstAvg = firstTwo.Average();
                var lastAvg = lastThree.Average();

                var threshold = 2.0; // 2 bpm threshold to avoid noise
                if (lastAvg > firstAvg + threshold)
                    return "Increasing";
                else if (lastAvg < firstAvg - threshold)
                    return "Decreasing";
            }

            return "Stable";
        }
    }
}
