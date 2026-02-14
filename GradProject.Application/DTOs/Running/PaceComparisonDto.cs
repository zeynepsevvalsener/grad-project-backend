namespace GradProject.Application.DTOs.Running
{
    public class PaceComparisonDto
    {
        public int ActivityId { get; set; }
        public DateTime ActivityDate { get; set; }
        public double Pace { get; set; } // minutes/km
        public double? DeltaFromPrevious { get; set; } // negative = improvement
        public double? ImprovementPercentage { get; set; }
    }
}

