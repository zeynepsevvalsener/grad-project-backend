namespace GradProject.Application.DTOs.Running
{
    public class HeartRateTrendResponseDto
    {
        public double? WeeklyAverageHeartRate { get; set; }
        public List<HeartRateDataPointDto> Last5RunsTrend { get; set; } = new();
        public string TrendDirection { get; set; } = null!; // "Increasing", "Decreasing", "Stable"
    }
}

