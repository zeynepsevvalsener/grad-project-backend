using NetTopologySuite.Geometries;

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

        public double? MaxHeartRate { get; set; }

        public double? MaxSpeedMetersPerSecond { get; set; }

        public double? AverageCadenceRpm { get; set; }

        /// <summary>Strava &quot;kilojoules&quot; (training load energy).</summary>
        public double? Kilojoules { get; set; }

        /// <summary>Strava activity elev_high (meters).</summary>
        public double? ElevHighMeters { get; set; }

        /// <summary>Strava activity elev_low (meters).</summary>
        public double? ElevLowMeters { get; set; }

        public bool HasHeartrate { get; set; }

        /// <summary>Strava relative effort / suffer score when present.</summary>
        public int? SufferScore { get; set; }

        /// <summary>Optional device name from source.</summary>
        public string? DeviceName { get; set; }

        /// <summary>JSON (jsonb): timezone, athlete count, gear id, map ids, polyline ids — extensible route metadata.</summary>
        public string? RouteMetadataJson { get; set; }

        public int? BurnedCalories { get; set; }

        public string Source { get; set; } = "STRAVA";

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public LineString? Route { get; set; }

        public double? MinLat { get; set; }

        public double? MaxLat { get; set; }

        public double? MinLng { get; set; }

        public double? MaxLng { get; set; }

        public Polygon? ConvexHull { get; set; }

        // Navigation
        public User User { get; set; } = null!;

        public ICollection<RunningActivitySplit> Splits { get; set; } = new List<RunningActivitySplit>();

        public RunningActivityAnalytics? Analytics { get; set; }
    }
}
