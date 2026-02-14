using System;
using System.Text.Json;

namespace GradProject.Infrastructure.Services.Strava
{
    /// <summary>
    /// Normalizes Strava API activity JSON into a consistent shape for RunningActivity.
    /// Applies fallbacks and cleanup so analytics get reliable data.
    /// </summary>
    public static class StravaActivityNormalizer
    {
        /// <summary>
        /// Result of normalizing a Strava activity. All required fields are guaranteed
        /// to have valid values; optional fields (e.g. calories, HR) may be null.
        /// </summary>
        public class NormalizedActivity
        {
            public string ExternalId { get; init; } = null!;
            public string Name { get; init; } = null!;
            public string Type { get; init; } = null!;
            public DateOnly RunDate { get; init; }
            public DateTimeOffset StartDateTime { get; init; }
            public int MovingTimeSeconds { get; init; }
            public int ElapsedTimeSeconds { get; init; }
            public float DistanceMeters { get; init; }
            public double TotalElevationGain { get; init; }
            public double AverageSpeed { get; init; }
            public double? AverageHeartRate { get; init; }
            public int? BurnedCalories { get; init; }
        }

        /// <summary>
        /// Normalizes a Strava activity JsonElement into values consistent with RunningActivity.
        /// Returns null if the activity is too invalid (e.g. missing id or dates).
        /// </summary>
        public static NormalizedActivity? Normalize(JsonElement activity)
        {
            if (activity.ValueKind != JsonValueKind.Object)
                return null;

            // --- ExternalId (required) ---
            if (!TryGetLong(activity, "id", out var id))
                return null;
            var externalId = id.ToString();

            // --- Date/Time: prefer local, fallback to UTC ---
            DateTimeOffset startDateTime;
            if (TryGetDateTimeOffset(activity, "start_date_local", out var localStart))
            {
                startDateTime = localStart;
            }
            else if (TryGetDateTimeOffset(activity, "start_date", out var utcStart))
            {
                startDateTime = utcStart;
            }
            else
            {
                return null;
            }

            var runDate = DateOnly.FromDateTime(startDateTime.Date);

            // --- Name: fallback to "Run" ---
            var name = GetString(activity, "name") ?? "Run";

            // --- Type: fallback to "Run" ---
            var type = GetString(activity, "type") ?? "Run";

            // --- Duration: prefer moving_time, fallback to elapsed_time, then 0 ---
            var movingTimeSeconds = GetIntNonNegative(activity, "moving_time")
                ?? GetIntNonNegative(activity, "elapsed_time")
                ?? 0;

            // --- Elapsed time: prefer elapsed_time, fallback to moving_time ---
            var elapsedTimeSeconds = GetIntNonNegative(activity, "elapsed_time")
                ?? movingTimeSeconds;

            // --- Distance: meters, non-negative ---
            var distanceMeters = GetFloatNonNegative(activity, "distance") ?? 0f;

            // --- Elevation gain: non-negative ---
            var totalElevationGain = GetDoubleNonNegative(activity, "total_elevation_gain") ?? 0.0;

            // --- Average speed: non-negative ---
            var averageSpeed = GetDoubleNonNegative(activity, "average_speed") ?? 0.0;

            // --- Average heart rate: optional ---
            double? averageHeartRate = null;
            if (TryGetNumber(activity, "average_heartrate", out var hr) && hr > 0)
            {
                averageHeartRate = hr;
            }

            // --- Calories: optional, non-negative if present ---
            int? burnedCalories = null;
            if (TryGetNumber(activity, "calories", out var cal))
            {
                var c = cal <= 0 ? (int?)null : (int)Math.Round(cal);
                if (c.HasValue && c.Value > 0)
                    burnedCalories = c;
            }

            return new NormalizedActivity
            {
                ExternalId = externalId,
                Name = name,
                Type = type,
                RunDate = runDate,
                StartDateTime = startDateTime,
                MovingTimeSeconds = movingTimeSeconds,
                ElapsedTimeSeconds = elapsedTimeSeconds,
                DistanceMeters = distanceMeters,
                TotalElevationGain = totalElevationGain,
                AverageSpeed = averageSpeed,
                AverageHeartRate = averageHeartRate,
                BurnedCalories = burnedCalories
            };
        }

        private static string? GetString(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var prop))
                return null;
            return prop.GetString();
        }

        private static bool TryGetLong(JsonElement element, string propertyName, out long value)
        {
            value = 0;
            if (!element.TryGetProperty(propertyName, out var prop))
                return false;
            if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt64(out value))
                return true;
            return false;
        }

        private static bool TryGetDateTimeOffset(JsonElement element, string propertyName, out DateTimeOffset result)
        {
            result = default;
            if (!element.TryGetProperty(propertyName, out var prop))
                return false;
            var s = prop.GetString();
            if (string.IsNullOrWhiteSpace(s))
                return false;
            return DateTimeOffset.TryParse(s, null, System.Globalization.DateTimeStyles.RoundtripKind, out result);
        }

        private static int? GetIntNonNegative(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var prop))
                return null;
            if (prop.ValueKind != JsonValueKind.Number)
                return null;
            if (!prop.TryGetInt32(out var v))
                return null;
            return v < 0 ? 0 : v;
        }

        private static float? GetFloatNonNegative(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var prop))
                return null;
            if (prop.ValueKind != JsonValueKind.Number)
                return null;
            float f;
            try
            {
                f = (float)prop.GetDouble();
            }
            catch
            {
                return null;
            }
            return f < 0 ? 0f : f;
        }

        private static double? GetDoubleNonNegative(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var prop))
                return null;
            if (prop.ValueKind != JsonValueKind.Number)
                return null;
            try
            {
                var d = prop.GetDouble();
                return d < 0 ? 0.0 : d;
            }
            catch
            {
                return null;
            }
        }

        private static bool TryGetNumber(JsonElement element, string propertyName, out double value)
        {
            value = 0;
            if (!element.TryGetProperty(propertyName, out var prop))
                return false;
            if (prop.ValueKind != JsonValueKind.Number)
                return false;
            try
            {
                value = prop.GetDouble();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
