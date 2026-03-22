namespace GradProject.Application.DTOs.Running
{
    public class WeeklyRunningSummaryDto
    {
        public int IsoYear { get; set; }
        public int IsoWeek { get; set; }
        public DateOnly WeekStart { get; set; }
        public DateOnly WeekEnd { get; set; }

        public int RunCount { get; set; }
        public int ActiveDays { get; set; }

        public double TotalDistanceMeters { get; set; }
        public int TotalMovingTimeSeconds { get; set; }
        public int TotalElapsedTimeSeconds { get; set; }
        public double TotalElevationGain { get; set; }
        public int? TotalCaloriesBurned { get; set; }
        public double? AverageHeartRate { get; set; }

        /// <summary>Overall week pace in minutes.seconds per km (same convention as pace-trend endpoints).</summary>
        public double AveragePaceMinutesPerKm { get; set; }

        public double LongestSingleRunDistanceMeters { get; set; }

        public IReadOnlyList<DailyRunningStatDto> DailyBreakdown { get; set; } = Array.Empty<DailyRunningStatDto>();

        public WeeklyRunningWeekOverWeekDto? VsPreviousIsoWeek { get; set; }
    }
}
