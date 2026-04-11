using GradProject.Application.DTOs.Common;

namespace GradProject.Application.Interfaces
{
    public interface IRunActivityService
    {
        Task<FetchLatestRunResult> FetchLatestStravaRunAsync(int userId, CancellationToken ct = default);
        Task<IReadOnlyList<RunActivityDto>> GetRecentAsync(int userId, int limit, DateOnly? startDate = null, DateOnly? endDate = null, CancellationToken ct = default);
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
        public string Name { get; set; } = null!;
        public string Type { get; set; } = null!;
        public DateOnly RunDate { get; set; }
        public DateTime StartTime { get; set; }
        public int MovingTimeSeconds { get; set; }
        public int ElapsedTimeSeconds { get; set; }
        public double DistanceMeters { get; set; }
        public double TotalElevationGain { get; set; }
        public double AverageSpeed { get; set; }
        public double? AverageHeartRate { get; set; }
        public double? MaxHeartRate { get; set; }
        public double? MaxSpeedMetersPerSecond { get; set; }
        public double? AverageCadenceRpm { get; set; }
        public double? Kilojoules { get; set; }
        public double? ElevHighMeters { get; set; }
        public double? ElevLowMeters { get; set; }
        public bool HasHeartrate { get; set; }
        public int? SufferScore { get; set; }
        public string? DeviceName { get; set; }
        public string? RouteMetadataJson { get; set; }
        public int? BurnedCalories { get; set; }
        public string Source { get; set; } = null!;
        public IReadOnlyList<RunActivitySplitDto>? Splits { get; set; }
        public RunActivityAnalyticsDto? Analytics { get; set; }
    }
}
