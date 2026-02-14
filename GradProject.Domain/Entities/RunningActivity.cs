namespace GradProject.Domain.Entities
{
    public class RunningActivity
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string ExternalActivityId { get; set; } = null!;

        public string Name { get; set; } = null!;

        public string Type { get; set; } = null!;

        public DateTime StartTime { get; set; }

        public DateOnly RunDate { get; set; }

        public double DistanceMeters { get; set; }

        public int MovingTimeSeconds { get; set; }

        public int ElapsedTimeSeconds { get; set; }

        public double TotalElevationGain { get; set; }

        public double AverageSpeed { get; set; }

        public double? AverageHeartRate { get; set; }

        public int? BurnedCalories { get; set; }

        public string Source { get; set; } = "STRAVA";

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        // Navigation
        public User User { get; set; } = null!;
    }
}
