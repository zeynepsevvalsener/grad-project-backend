using System.Globalization;
using GradProject.Domain.Entities;
using GradProject.Infrastructure.Persistence;
using GradProject.Infrastructure.Services.Running;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace GradProject.Tests;

public class WeeklyRunningSummaryServiceTests
{
    private static AppDbContext CreateDb(string name)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: name)
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        var db = new AppDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    private static User SeedUser(AppDbContext db, int id)
    {
        var user = new User
        {
            Id = id,
            Email = $"u{id}@test.com",
            PasswordHash = Array.Empty<byte>(),
            PasswordSalt = Array.Empty<byte>()
        };
        db.Users.Add(user);
        return user;
    }

    private static void SeedRun(
        AppDbContext db,
        int id,
        int userId,
        DateOnly runDate,
        double distanceMeters,
        int movingTimeSeconds,
        int? burnedCalories = null,
        double? averageHeartRate = null)
    {
        db.RunningActivities.Add(new RunningActivity
        {
            Id = id,
            UserId = userId,
            ExternalActivityId = $"ext_{id}",
            Name = "Run",
            Type = "Run",
            StartTime = runDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            RunDate = runDate,
            DistanceMeters = distanceMeters,
            MovingTimeSeconds = movingTimeSeconds,
            ElapsedTimeSeconds = movingTimeSeconds,
            TotalElevationGain = 10,
            AverageSpeed = distanceMeters / movingTimeSeconds,
            AverageHeartRate = averageHeartRate,
            BurnedCalories = burnedCalories,
            Source = "STRAVA",
            CreatedAt = runDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            UpdatedAt = runDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
        });
    }

    [Fact]
    public async Task Aggregates_totals_and_daily_breakdown_for_iso_week()
    {
        using var db = CreateDb(nameof(Aggregates_totals_and_daily_breakdown_for_iso_week));
        SeedUser(db, 1);
        var monday = DateOnly.FromDateTime(ISOWeek.ToDateTime(2025, 12, DayOfWeek.Monday));
        SeedRun(db, 1, 1, monday, 5000, 1500);
        SeedRun(db, 2, 1, monday.AddDays(1), 3000, 900);
        SeedRun(db, 3, 1, monday.AddDays(8), 9999, 1);
        await db.SaveChangesAsync();

        var sut = new WeeklyRunningSummaryService(db);
        var result = await sut.GetWeeklySummaryAsync(1, 2025, 12, includeWeekOverWeek: false);

        Assert.Equal(2025, result.IsoYear);
        Assert.Equal(12, result.IsoWeek);
        Assert.Equal(monday, result.WeekStart);
        Assert.Equal(monday.AddDays(6), result.WeekEnd);
        Assert.Equal(2, result.RunCount);
        Assert.Equal(2, result.ActiveDays);
        Assert.Equal(8000, result.TotalDistanceMeters);
        Assert.Equal(2400, result.TotalMovingTimeSeconds);
        Assert.Equal(20, result.TotalElevationGain);
        Assert.Null(result.VsPreviousIsoWeek);
        Assert.Equal(2, result.DailyBreakdown.Count);
        Assert.Equal(5000, result.DailyBreakdown[0].DistanceMeters);
        Assert.Equal(3000, result.DailyBreakdown[1].DistanceMeters);
    }

    [Fact]
    public async Task Week_over_week_includes_previous_week_and_percent_change()
    {
        using var db = CreateDb(nameof(Week_over_week_includes_previous_week_and_percent_change));
        SeedUser(db, 1);
        var w13Monday = DateOnly.FromDateTime(ISOWeek.ToDateTime(2025, 13, DayOfWeek.Monday));
        var w12Monday = w13Monday.AddDays(-7);
        SeedRun(db, 1, 1, w12Monday, 10000, 3000);
        SeedRun(db, 2, 1, w13Monday, 5000, 1500);
        await db.SaveChangesAsync();

        var sut = new WeeklyRunningSummaryService(db);
        var result = await sut.GetWeeklySummaryAsync(1, 2025, 13, includeWeekOverWeek: true);

        Assert.NotNull(result.VsPreviousIsoWeek);
        Assert.Equal(10000, result.VsPreviousIsoWeek!.PreviousTotalDistanceMeters);
        Assert.Equal(1, result.VsPreviousIsoWeek.PreviousRunCount);
        Assert.NotNull(result.VsPreviousIsoWeek.DistanceChangePercent);
        Assert.Equal(-50, result.VsPreviousIsoWeek.DistanceChangePercent);
    }

    [Fact]
    public async Task Throws_when_only_one_of_year_or_week_provided()
    {
        using var db = CreateDb(nameof(Throws_when_only_one_of_year_or_week_provided));
        var sut = new WeeklyRunningSummaryService(db);
        await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.GetWeeklySummaryAsync(1, 2025, null, false));
    }

    [Fact]
    public async Task Throws_when_iso_week_invalid_for_year()
    {
        using var db = CreateDb(nameof(Throws_when_iso_week_invalid_for_year));
        var sut = new WeeklyRunningSummaryService(db);
        var weeksIn2027 = ISOWeek.GetWeeksInYear(2027);
        Assert.True(weeksIn2027 is 52 or 53);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            sut.GetWeeklySummaryAsync(1, 2027, 54, false));
    }
}
