namespace GradProject.Domain.Entities
{
    public class RunActivity
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string ExternalId { get; set; } = null!;

        public DateOnly RunDate { get; set; }

        public DateTimeOffset StartDateTime { get; set; }

        public int DurationSeconds { get; set; }

        public float DistanceMeters { get; set; }

        public int? BurnedCalories { get; set; }

        public string Source { get; set; } = null!;

        // Navigation
        public User User { get; set; } = null!;
    }
}

