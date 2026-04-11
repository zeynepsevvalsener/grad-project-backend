using System;
using System.Collections.Generic;
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
            public string? SummaryPolyline { get; init; }

            public double? MaxSpeedMetersPerSecond { get; init; }
            public double? MaxHeartRate { get; init; }
            public double? AverageCadenceRpm { get; init; }
            public double? Kilojoules { get; init; }
            public double? ElevHighMeters { get; init; }
            public double? ElevLowMeters { get; init; }
            public bool HasHeartrate { get; init; }
            public int? SufferScore { get; init; }
            public string? DeviceName { get; init; }
            public string? RouteMetadataJson { get; init; }

            public IReadOnlyList<NormalizedSplit> Splits { get; init; } = Array.Empty<NormalizedSplit>();
        }

        public sealed class NormalizedSplit
        {
            public float DistanceMeters { get; init; }
            public int MovingTimeSeconds { get; init; }
            public int ElapsedTimeSeconds { get; init; }
            public double ElevationDifferenceMeters { get; init; }
            public double AverageSpeedMetersPerSecond { get; init; }
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

            // --- Summary Polyline: optional, from map.summary_polyline ---
            string? summaryPolyline = null;
            activity.TryGetProperty("map", out var mapElement);
            if (mapElement.ValueKind == JsonValueKind.Object)
                summaryPolyline = GetString(mapElement, "summary_polyline");

            var maxSpeed = GetDoubleNonNegative(activity, "max_speed");
            double? maxHr = null;
            if (TryGetNumber(activity, "max_heartrate", out var mxHr) && mxHr > 0)
                maxHr = mxHr;
            var avgCadence = GetDoubleNonNegative(activity, "average_cadence");
            var kilojoules = GetDoubleNonNegative(activity, "kilojoules");
            double? elevHigh = TryGetSignedDouble(activity, "elev_high");
            double? elevLow = TryGetSignedDouble(activity, "elev_low");
            var hasHr = activity.TryGetProperty("has_heartrate", out var hrProp) &&
                          hrProp.ValueKind == JsonValueKind.True;
            int? suffer = GetIntPositive(activity, "suffer_score") ?? GetIntPositive(activity, "perceived_exertion");
            var deviceName = GetString(activity, "device_name");

            var splits = ParseSplitsMetric(activity);
            var routeMetaJson = BuildRouteMetadataJson(activity, mapElement);

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
                BurnedCalories = burnedCalories,
                SummaryPolyline = summaryPolyline,
                MaxSpeedMetersPerSecond = maxSpeed,
                MaxHeartRate = maxHr,
                AverageCadenceRpm = avgCadence,
                Kilojoules = kilojoules,
                ElevHighMeters = elevHigh,
                ElevLowMeters = elevLow,
                HasHeartrate = hasHr,
                SufferScore = suffer,
                DeviceName = deviceName,
                RouteMetadataJson = routeMetaJson,
                Splits = splits
            };
        }

        private static IReadOnlyList<NormalizedSplit> ParseSplitsMetric(JsonElement activity)
        {
            if (!activity.TryGetProperty("splits_metric", out var arr) || arr.ValueKind != JsonValueKind.Array)
                return Array.Empty<NormalizedSplit>();

            var list = new List<NormalizedSplit>();
            foreach (var split in arr.EnumerateArray())
            {
                if (split.ValueKind != JsonValueKind.Object)
                    continue;
                var dist = GetFloatNonNegative(split, "distance") ?? 0f;
                var mov = GetIntNonNegative(split, "moving_time") ?? 0;
                var ela = GetIntNonNegative(split, "elapsed_time") ?? mov;
                var elDiff = GetDoubleNonNegative(split, "elevation_difference") ?? 0.0;
                var avgSp = GetDoubleNonNegative(split, "average_speed") ?? 0.0;
                list.Add(new NormalizedSplit
                {
                    DistanceMeters = dist,
                    MovingTimeSeconds = mov,
                    ElapsedTimeSeconds = ela,
                    ElevationDifferenceMeters = elDiff,
                    AverageSpeedMetersPerSecond = avgSp
                });
            }

            return list;
        }

        private static string? BuildRouteMetadataJson(JsonElement activity, JsonElement mapElement)
        {
            var dict = new Dictionary<string, object?>(StringComparer.Ordinal);
            var tz = GetString(activity, "timezone");
            if (!string.IsNullOrEmpty(tz))
                dict["timezone"] = tz;
            var gear = GetString(activity, "gear_id");
            if (!string.IsNullOrEmpty(gear))
                dict["gearId"] = gear;
            if (activity.TryGetProperty("trainer", out var tr) && tr.ValueKind is JsonValueKind.True or JsonValueKind.False)
                dict["trainer"] = tr.GetBoolean();
            if (activity.TryGetProperty("commute", out var co) && co.ValueKind is JsonValueKind.True or JsonValueKind.False)
                dict["commute"] = co.GetBoolean();
            if (activity.TryGetProperty("manual", out var man) && man.ValueKind is JsonValueKind.True or JsonValueKind.False)
                dict["manual"] = man.GetBoolean();
            if (mapElement.ValueKind == JsonValueKind.Object)
            {
                var id = GetString(mapElement, "id");
                if (!string.IsNullOrEmpty(id))
                    dict["mapId"] = id;
            }

            if (dict.Count == 0)
                return null;
            return JsonSerializer.Serialize(dict);
        }

        private static double? TryGetSignedDouble(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var prop))
                return null;
            if (prop.ValueKind != JsonValueKind.Number)
                return null;
            try
            {
                return prop.GetDouble();
            }
            catch
            {
                return null;
            }
        }

        private static int? GetIntPositive(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out var prop))
                return null;
            if (prop.ValueKind != JsonValueKind.Number || !prop.TryGetInt32(out var v))
                return null;
            return v <= 0 ? null : v;
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
