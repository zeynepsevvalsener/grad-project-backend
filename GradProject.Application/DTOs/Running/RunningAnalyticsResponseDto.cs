namespace GradProject.Application.DTOs.Running
{
    public class RunningAnalyticsResponseDto
    {
        public PaceTrendResponseDto PaceTrend { get; set; } = null!;
        public HeartRateTrendResponseDto HeartRateTrend { get; set; } = null!;
        public DateTime CalculatedAt { get; set; }
    }
}

