using GradProject.Application.DTOs.Running;
using GradProject.Application.Interfaces.Running;
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

            var totalSecondsList = new List<double>();
            foreach (var activity in activitiesForWeeklyAvg)
            {
                // Calculate total seconds per km for averaging
                var totalSecondsPerKm = (activity.MovingTimeSeconds / activity.DistanceMeters) * 1000.0;
                if (totalSecondsPerKm > 0)
                {
                    totalSecondsList.Add(totalSecondsPerKm);
                }
            }

            // Calculate average in seconds, then convert to minutes.seconds format
            var weeklyAveragePace = 0.0;
            if (totalSecondsList.Count > 0)
            {
                var avgSecondsPerKm = totalSecondsList.Average();
                var minutes = Math.Floor(avgSecondsPerKm / 60.0);
                var seconds = avgSecondsPerKm % 60.0;
                if (seconds >= 60.0)
                {
                    minutes += Math.Floor(seconds / 60.0);
                    seconds = seconds % 60.0;
                }
                weeklyAveragePace = Math.Round(minutes + (seconds / 100.0), 2, MidpointRounding.AwayFromZero);
            }

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
                var pace = CalculatePace(activity.DistanceMeters, activity.MovingTimeSeconds);

                if (i == 0)
                    firstPace = pace;
                if (i == last5ValidActivities.Count - 1)
                    lastPace = pace;

                // Delta is the difference between current and previous pace
                // Since both are in minutes.seconds format, we can subtract directly
                double? delta = previousPace.HasValue ? Math.Round(pace - previousPace.Value, 2, MidpointRounding.AwayFromZero) : (double?)null;
                double? improvementPercentage = previousPace.HasValue && previousPace.Value > 0
                    ? Math.Round(CalculateImprovementPercentage(previousPace.Value, pace), 2, MidpointRounding.AwayFromZero)
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
                overallImprovement = Math.Round(CalculateImprovementPercentage(firstPace.Value, lastPace.Value), 2, MidpointRounding.AwayFromZero);
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
                    var pace = CalculatePace(activity.DistanceMeters, activity.MovingTimeSeconds);
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
        /// Calculates pace in minutes per kilometer (how many minutes to run 1 km)
        /// Format: minutes.seconds (e.g., 5.45 means 5 minutes and 45 seconds)
        /// The decimal part represents seconds (0-59), not fractional minutes
        /// Formula: totalSecondsPerKm = (movingTimeSeconds / distanceMeters) * 1000
        /// Returns value in format minutes.seconds rounded to 2 decimal places
        /// </summary>
        private double CalculatePace(double distanceMeters, int movingTimeSeconds)
        {
            if (distanceMeters <= 0 || movingTimeSeconds <= 0)
                return 0.0;

            // Calculate total seconds per kilometer
            var totalSecondsPerKm = (movingTimeSeconds / distanceMeters) * 1000.0;
            
            // Extract minutes and seconds
            var minutes = Math.Floor(totalSecondsPerKm / 60.0);
            var seconds = totalSecondsPerKm % 60.0;
            
            // Ensure seconds don't exceed 59 (shouldn't happen, but safety check)
            if (seconds >= 60.0)
            {
                minutes += Math.Floor(seconds / 60.0);
                seconds = seconds % 60.0;
            }
            
            // Format as minutes.seconds (e.g., 5.45 for 5 minutes 45 seconds)
            var pace = minutes + (seconds / 100.0);
            return Math.Round(pace, 2, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Calculates improvement percentage
        /// Formula: ((oldPaceSeconds - newPaceSeconds) / oldPaceSeconds) * 100
        /// Positive = improvement (faster), Negative = regression (slower)
        /// Pace values are in minutes.seconds format, so we convert to seconds first
        /// </summary>
        private double CalculateImprovementPercentage(double oldPace, double newPace)
        {
            if (oldPace <= 0)
                return 0.0;

            // Convert pace from minutes.seconds format to total seconds
            var oldMinutes = Math.Floor(oldPace);
            var oldSeconds = (oldPace - oldMinutes) * 100.0;
            var oldPaceSeconds = (oldMinutes * 60.0) + oldSeconds;

            var newMinutes = Math.Floor(newPace);
            var newSeconds = (newPace - newMinutes) * 100.0;
            var newPaceSeconds = (newMinutes * 60.0) + newSeconds;

            if (oldPaceSeconds <= 0)
                return 0.0;

            return ((oldPaceSeconds - newPaceSeconds) / oldPaceSeconds) * 100.0;
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
