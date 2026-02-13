using System.Text.Json;
using GradProject.Infrastructure.Services.Strava;
using Xunit;

namespace GradProject.Tests;

public class StravaActivityNormalizerTests
{
    private static JsonElement Parse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    [Fact]
    public void Normalize_ValidActivity_ReturnsAllFields()
    {
        var json = """
            {
                "id": 12345678,
                "start_date_local": "2026-02-14T08:30:00Z",
                "start_date": "2026-02-14T06:30:00Z",
                "moving_time": 1800,
                "elapsed_time": 1920,
                "distance": 5000.5,
                "calories": 320.7,
                "type": "Run"
            }
            """;
        var activity = Parse(json);

        var result = StravaActivityNormalizer.Normalize(activity);

        Assert.NotNull(result);
        Assert.Equal("12345678", result.ExternalId);
        Assert.Equal(new DateOnly(2026, 2, 14), result.RunDate);
        Assert.Equal(1800, result.DurationSeconds); // moving_time preferred
        Assert.Equal(5000.5f, result.DistanceMeters);
        Assert.Equal(321, result.BurnedCalories); // rounded, positive
    }

    [Fact]
    public void Normalize_FallbackToStartDate_WhenStartDateLocalMissing()
    {
        var json = """
            {
                "id": 1,
                "start_date": "2026-01-01T12:00:00Z",
                "moving_time": 600,
                "distance": 1000
            }
            """;
        var activity = Parse(json);

        var result = StravaActivityNormalizer.Normalize(activity);

        Assert.NotNull(result);
        Assert.Equal(new DateOnly(2026, 1, 1), result.RunDate);
        Assert.Equal(600, result.DurationSeconds);
        Assert.Equal(1000f, result.DistanceMeters);
    }

    [Fact]
    public void Normalize_FallbackToElapsedTime_WhenMovingTimeMissing()
    {
        var json = """
            {
                "id": 2,
                "start_date_local": "2026-02-14T09:00:00Z",
                "elapsed_time": 2400,
                "distance": 6000
            }
            """;
        var activity = Parse(json);

        var result = StravaActivityNormalizer.Normalize(activity);

        Assert.NotNull(result);
        Assert.Equal(2400, result.DurationSeconds);
    }

    [Fact]
    public void Normalize_MissingId_ReturnsNull()
    {
        var json = """
            {
                "start_date_local": "2026-02-14T09:00:00Z",
                "moving_time": 600,
                "distance": 1000
            }
            """;
        var activity = Parse(json);

        var result = StravaActivityNormalizer.Normalize(activity);

        Assert.Null(result);
    }

    [Fact]
    public void Normalize_MissingDates_ReturnsNull()
    {
        var json = """
            {
                "id": 3,
                "moving_time": 600,
                "distance": 1000
            }
            """;
        var activity = Parse(json);

        var result = StravaActivityNormalizer.Normalize(activity);

        Assert.Null(result);
    }

    [Fact]
    public void Normalize_NegativeDistance_ClampedToZero()
    {
        var json = """
            {
                "id": 4,
                "start_date_local": "2026-02-14T09:00:00Z",
                "moving_time": 600,
                "distance": -100
            }
            """;
        var activity = Parse(json);

        var result = StravaActivityNormalizer.Normalize(activity);

        Assert.NotNull(result);
        Assert.Equal(0f, result.DistanceMeters);
    }

    [Fact]
    public void Normalize_NegativeCalories_StoredAsNull()
    {
        var json = """
            {
                "id": 5,
                "start_date_local": "2026-02-14T09:00:00Z",
                "moving_time": 600,
                "distance": 2000,
                "calories": -50
            }
            """;
        var activity = Parse(json);

        var result = StravaActivityNormalizer.Normalize(activity);

        Assert.NotNull(result);
        Assert.Null(result.BurnedCalories);
    }

    [Fact]
    public void Normalize_NoDuration_UsesZero()
    {
        var json = """
            {
                "id": 6,
                "start_date_local": "2026-02-14T09:00:00Z",
                "distance": 3000
            }
            """;
        var activity = Parse(json);

        var result = StravaActivityNormalizer.Normalize(activity);

        Assert.NotNull(result);
        Assert.Equal(0, result.DurationSeconds);
    }
}
