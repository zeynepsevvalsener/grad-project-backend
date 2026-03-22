namespace GradProject.Application.DTOs.Running
{
    public class DailyRunningStatDto
    {
        public DateOnly Date { get; set; }
        public int RunCount { get; set; }
        public double DistanceMeters { get; set; }
        public int MovingTimeSeconds { get; set; }
    }
}
