namespace GradProject.Application.DTOs.Running
{
    public class PaceTrendResponseDto
    {
        public double WeeklyAveragePace { get; set; } // minutes/km
        public List<PaceComparisonDto> Last5RunsComparison { get; set; } = new();
        public double? OverallImprovementPercentage { get; set; }
    }
}

