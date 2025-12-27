namespace GradProject.Application.Interfaces
{
    public interface IRunActivityService
    {
        Task<FetchLatestRunResult> FetchLatestStravaRunAsync(int userId, CancellationToken ct = default);
    }

    public class FetchLatestRunResult
    {
        public bool Success { get; set; }
        public bool RequiresStravaConnection { get; set; }
        public string? ErrorMessage { get; set; }
        public RunActivityDto? RunActivity { get; set; }
    }

    public class RunActivityDto
    {
        public int Id { get; set; }
        public string ExternalId { get; set; } = null!;
        public DateOnly RunDate { get; set; }
        public DateTimeOffset StartDateTime { get; set; }
        public int DurationSeconds { get; set; }
        public float DistanceMeters { get; set; }
        public int? BurnedCalories { get; set; }
        public string Source { get; set; } = null!;
    }
}

