namespace GradProject.Application.DTOs.Running
{
    public class HeartRateDataPointDto
    {
        public int ActivityId { get; set; }
        public DateTime ActivityDate { get; set; }
        public double AverageHeartRate { get; set; }
        public double Pace { get; set; } // minutes/km (for correlation)
    }
}

