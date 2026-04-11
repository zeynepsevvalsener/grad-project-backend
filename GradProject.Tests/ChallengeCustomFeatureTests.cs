using GradProject.Application.Configuration;
using GradProject.Application.DTOs.Gamification;
using GradProject.Application.Exceptions;
using GradProject.Application.Interfaces;
using GradProject.Application.Interfaces.Gamification;
using GradProject.Application.Validators.Gamification;
using GradProject.Domain.Entities;
using GradProject.Domain.Enums;
using GradProject.Infrastructure.Persistence;
using GradProject.Infrastructure.Services.Gamification;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace GradProject.Tests;

internal sealed class FixedOptionsSnapshot<T> : Microsoft.Extensions.Options.IOptionsSnapshot<T>
    where T : class
{
    public FixedOptionsSnapshot(T value) => Value = value;

    public T Value { get; }

    public T Get(string? name) => Value;
}

public class ChallengeCustomFeatureTests
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

    private static ChallengeService CreateChallengeService(AppDbContext db, CustomChallengeSettings? settings = null)
    {
        settings ??= new CustomChallengeSettings();
        return new ChallengeService(db, new MemoryCache(new MemoryCacheOptions()), Options.Create(settings));
    }

    private static void SeedUser(AppDbContext db, int id = 1)
    {
        db.Users.Add(new User
        {
            Id = id,
            Email = $"u{id}@t.com",
            PasswordHash = [],
            PasswordSalt = []
        });
    }

    private sealed class NoOpBadgeEvaluation : IBadgeEvaluationService
    {
        public Task<BadgeEvaluationResult> EvaluateBadgeConditionsAsync(int userId, CancellationToken ct = default) =>
            Task.FromResult(BadgeEvaluationResult.Empty);
    }

    private sealed class SpyAchievementPublisher : IAchievementEventPublisher
    {
        public List<AchievementEventDto> Published { get; } = new();

        public Task PublishAsync(AchievementEventDto evt, CancellationToken ct = default)
        {
            Published.Add(evt);
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task CreateCustomAsync_Distance_SetsCustomMetadataAndRunningDistance()
    {
        var db = CreateDb(nameof(CreateCustomAsync_Distance_SetsCustomMetadataAndRunningDistance));
        SeedUser(db);
        await db.SaveChangesAsync();

        var svc = CreateChallengeService(db);
        var created = await svc.CreateCustomAsync(1, new CreateCustomChallengeRequestDto
        {
            Title = " My Run ",
            Description = " desc ",
            GoalType = CustomChallengeGoalType.Distance,
            TargetValue = 5000
        });

        Assert.True(created.IsCustom);
        Assert.Equal(1, created.CreatedByUserId);
        Assert.Equal(ChallengeType.Running, created.Type);
        Assert.Equal(ChallengeMetric.Distance, created.Metric);
        Assert.Equal(0, created.RewardPoints);
        Assert.True(created.IsActive);

        var row = await db.Challenges.AsNoTracking().SingleAsync(c => c.Id == created.Id);
        Assert.True(row.IsCustom);
        Assert.Equal(1, row.CreatedByUserId);
    }

    [Fact]
    public async Task CreateCustomAsync_ThrowsWhenEndNotInFuture()
    {
        var db = CreateDb(nameof(CreateCustomAsync_ThrowsWhenEndNotInFuture));
        SeedUser(db);
        await db.SaveChangesAsync();

        var svc = CreateChallengeService(db);
        var past = DateTime.UtcNow.AddDays(-5);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.CreateCustomAsync(1, new CreateCustomChallengeRequestDto
            {
                Title = "T",
                GoalType = CustomChallengeGoalType.Distance,
                TargetValue = 1000,
                StartDate = past.AddDays(-10),
                EndDate = past
            }));
    }

    [Fact]
    public async Task CreateCustomAsync_EnforcesRateLimitPer24Hours()
    {
        var db = CreateDb(nameof(CreateCustomAsync_EnforcesRateLimitPer24Hours));
        SeedUser(db);
        await db.SaveChangesAsync();

        var settings = new CustomChallengeSettings { MaxCreatesPer24Hours = 2 };
        var svc = CreateChallengeService(db, settings);

        await svc.CreateCustomAsync(1, new CreateCustomChallengeRequestDto
        {
            Title = "A",
            GoalType = CustomChallengeGoalType.Distance,
            TargetValue = 1000
        });
        await svc.CreateCustomAsync(1, new CreateCustomChallengeRequestDto
        {
            Title = "B",
            GoalType = CustomChallengeGoalType.Distance,
            TargetValue = 1000
        });

        await Assert.ThrowsAsync<ChallengeCreationRateLimitExceededException>(() =>
            svc.CreateCustomAsync(1, new CreateCustomChallengeRequestDto
            {
                Title = "C",
                GoalType = CustomChallengeGoalType.Distance,
                TargetValue = 1000
            }));
    }

    [Fact]
    public async Task JoinChallengeAsync_WhenAlreadyJoined_SucceedsEvenIfChallengeExpired()
    {
        var db = CreateDb(nameof(JoinChallengeAsync_WhenAlreadyJoined_SucceedsEvenIfChallengeExpired));
        SeedUser(db);
        db.Challenges.Add(new Challenge
        {
            Title = "Ended",
            Type = ChallengeType.Running,
            Metric = ChallengeMetric.Distance,
            TargetValue = 1000,
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow.AddDays(-1),
            RewardPoints = 5,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-30)
        });
        await db.SaveChangesAsync();
        var challengeId = (await db.Challenges.SingleAsync()).Id;

        db.UserChallenges.Add(new UserChallenge
        {
            UserId = 1,
            ChallengeId = challengeId,
            JoinedAt = DateTime.UtcNow.AddDays(-10),
            ProgressDistanceMeters = 0,
            ProgressCalories = 0,
            Completed = false
        });
        await db.SaveChangesAsync();

        var svc = CreateChallengeService(db);
        var dto = await svc.JoinChallengeAsync(1, challengeId);

        Assert.Equal(challengeId, dto.ChallengeId);
    }

    [Fact]
    public async Task JoinChallengeAsync_ThrowsWhenOutsideDateWindow()
    {
        var db = CreateDb(nameof(JoinChallengeAsync_ThrowsWhenOutsideDateWindow));
        SeedUser(db);
        db.Challenges.Add(new Challenge
        {
            Title = "Old",
            Type = ChallengeType.Running,
            Metric = ChallengeMetric.Distance,
            TargetValue = 1000,
            StartDate = DateTime.UtcNow.AddDays(-30),
            EndDate = DateTime.UtcNow.AddDays(-1),
            RewardPoints = 5,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-30)
        });
        await db.SaveChangesAsync();

        var svc = CreateChallengeService(db);
        var challengeId = (await db.Challenges.SingleAsync()).Id;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.JoinChallengeAsync(1, challengeId));
    }

    [Fact]
    public async Task UpdateAsync_PreservesCustomCreatorFields()
    {
        var db = CreateDb(nameof(UpdateAsync_PreservesCustomCreatorFields));
        SeedUser(db);
        await db.SaveChangesAsync();

        var svc = CreateChallengeService(db);
        var created = await svc.CreateCustomAsync(1, new CreateCustomChallengeRequestDto
        {
            Title = "Original",
            GoalType = CustomChallengeGoalType.Calories,
            TargetValue = 500
        });

        var originalCreatedAt = (await db.Challenges.AsNoTracking().SingleAsync(c => c.Id == created.Id)).CreatedAtUtc;

        await svc.UpdateAsync(created.Id, new UpdateChallengeRequestDto
        {
            Title = "Renamed",
            Description = "d",
            Type = ChallengeType.Nutrition,
            Metric = ChallengeMetric.Calories,
            TargetValue = 600,
            StartDate = created.StartDate,
            EndDate = created.EndDate,
            RewardPoints = 99,
            IsActive = true
        });

        var row = await db.Challenges.AsNoTracking().SingleAsync(c => c.Id == created.Id);
        Assert.True(row.IsCustom);
        Assert.Equal(1, row.CreatedByUserId);
        Assert.Equal(originalCreatedAt, row.CreatedAtUtc);
        Assert.Equal("Renamed", row.Title);
    }

    [Fact]
    public async Task UpdateAfterRunSaved_CompletesCustomDistanceChallenge_AndPublishesEvent()
    {
        var db = CreateDb(nameof(UpdateAfterRunSaved_CompletesCustomDistanceChallenge_AndPublishesEvent));
        SeedUser(db);
        var now = DateTime.UtcNow;
        db.Challenges.Add(new Challenge
        {
            Title = "Custom run",
            Type = ChallengeType.Running,
            Metric = ChallengeMetric.Distance,
            TargetValue = 100,
            StartDate = now.AddDays(-1),
            EndDate = now.AddDays(7),
            RewardPoints = 0,
            IsActive = true,
            IsCustom = true,
            CreatedByUserId = 1,
            CreatedAtUtc = now
        });
        await db.SaveChangesAsync();
        var challengeId = (await db.Challenges.SingleAsync()).Id;

        db.UserChallenges.Add(new UserChallenge
        {
            UserId = 1,
            ChallengeId = challengeId,
            JoinedAt = now,
            ProgressDistanceMeters = 0,
            ProgressCalories = 0,
            Completed = false
        });
        db.RunningActivities.Add(new RunningActivity
        {
            UserId = 1,
            ExternalActivityId = "ext-1",
            Name = "Run",
            Type = "Run",
            StartTime = now,
            RunDate = DateOnly.FromDateTime(now),
            DistanceMeters = 150,
            MovingTimeSeconds = 60,
            ElapsedTimeSeconds = 60,
            TotalElevationGain = 0,
            AverageSpeed = 2,
            Source = "TEST",
            CreatedAt = now,
            UpdatedAt = now
        });
        await db.SaveChangesAsync();
        var runId = (await db.RunningActivities.SingleAsync()).Id;

        var spy = new SpyAchievementPublisher();
        var progress = new ChallengeProgressService(
            db,
            new NoOpBadgeEvaluation(),
            spy,
            NullLogger<ChallengeProgressService>.Instance);

        await progress.UpdateAfterRunSaved(runId);

        var uc = await db.UserChallenges.SingleAsync();
        Assert.True(uc.Completed);
        Assert.Single(spy.Published);
        Assert.Equal(AchievementEventType.ChallengeCompleted, spy.Published[0].Type);
        Assert.Equal(challengeId, spy.Published[0].ChallengeId);
    }

    [Fact]
    public async Task UpdateAfterNutritionSaved_CompletesCustomCaloriesChallenge()
    {
        var db = CreateDb(nameof(UpdateAfterNutritionSaved_CompletesCustomCaloriesChallenge));
        SeedUser(db);
        var now = DateTime.UtcNow;
        db.Challenges.Add(new Challenge
        {
            Title = "Eat",
            Type = ChallengeType.Nutrition,
            Metric = ChallengeMetric.Calories,
            TargetValue = 100,
            StartDate = now.AddDays(-1),
            EndDate = now.AddDays(7),
            RewardPoints = 0,
            IsActive = true,
            IsCustom = true,
            CreatedByUserId = 1,
            CreatedAtUtc = now
        });
        await db.SaveChangesAsync();
        var challengeId = (await db.Challenges.SingleAsync()).Id;

        db.UserChallenges.Add(new UserChallenge
        {
            UserId = 1,
            ChallengeId = challengeId,
            JoinedAt = now,
            ProgressDistanceMeters = 0,
            ProgressCalories = 0,
            Completed = false
        });
        await db.SaveChangesAsync();

        var progress = new ChallengeProgressService(
            db,
            new NoOpBadgeEvaluation(),
            new SpyAchievementPublisher(),
            NullLogger<ChallengeProgressService>.Instance);

        var date = DateOnly.FromDateTime(now);
        await progress.UpdateAfterNutritionSaved(1, 150, date);

        var uc = await db.UserChallenges.SingleAsync();
        Assert.True(uc.Completed);
    }

    [Fact]
    public async Task CreateCustomChallengeRequestDtoValidator_RejectsInvalidTarget()
    {
        var validator = new CreateCustomChallengeRequestDtoValidator(
            new FixedOptionsSnapshot<CustomChallengeSettings>(new CustomChallengeSettings()));
        var result = await validator.ValidateAsync(new CreateCustomChallengeRequestDto
        {
            Title = "Ok",
            GoalType = CustomChallengeGoalType.Distance,
            TargetValue = 0
        });

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateCustomChallengeRequestDto.TargetValue));
    }
}
