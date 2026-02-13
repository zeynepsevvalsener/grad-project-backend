using System;
using System.Text.Json;

namespace GradProject.Infrastructure.Services.Strava
{
    /// <summary>
    /// Normalizes Strava API activity JSON into a consistent shape for RunActivity.
    /// Applies fallbacks and cleanup so analytics get reliable data.
    /// </summary>
    public static class StravaActivityNormalizer
    {
        /// <summary>
        /// Result of normalizing a Strava activity. All required fields are guaranteed
        /// to have valid values; optional fields (e.g. calories) may be null.
        /// </summary>
        public class NormalizedActivity
        {
            public string ExternalId { get; init; } = null!;
            public DateOnly RunDate { get; init; }
            public DateTimeOffset StartDateTime { get; init; }
            public int DurationSeconds { get; init; }
            public float DistanceMeters { get; init; }
            public int? BurnedCalories { get; init; }
        }

        /// <summary>
        /// Normalizes a Strava activity JsonElement into values consistent with RunActivity.
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

            // --- Duration: prefer moving_time, fallback to elapsed_time, then 0 ---
            var durationSeconds = GetIntNonNegative(activity, "moving_time")
                ?? GetIntNonNegative(activity, "elapsed_time")
                ?? 0;

            // --- Distance: meters, non-negative ---
            var distanceMeters = GetFloatNonNegative(activity, "distance") ?? 0f;

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
                RunDate = runDate,
                StartDateTime = startDateTime,
                DurationSeconds = durationSeconds,
                DistanceMeters = distanceMeters,
                BurnedCalories = burnedCalories
            };
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
