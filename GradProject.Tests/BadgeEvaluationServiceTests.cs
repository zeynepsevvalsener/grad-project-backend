using GradProject.Application.DTOs.Gamification;
using GradProject.Application.Interfaces;
using GradProject.Domain.Entities;
using GradProject.Domain.Enums;
using GradProject.Infrastructure.Persistence;
using GradProject.Infrastructure.Services.Gamification;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace GradProject.Tests;

/// <summary>
/// Tests for BadgeEvaluationService.EvaluateBadgeConditionsAsync.
/// Uses EF Core InMemory — no real DB required.
/// Each test gets its own isolated DB to prevent cross-test state.
/// </summary>
public class BadgeEvaluationServiceTests
{
    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

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

    private static BadgeEvaluationService CreateService(
        AppDbContext db,
        IAchievementEventPublisher? publisher = null)
    {
        var badgeService = new BadgeService(db, NullLogger<BadgeService>.Instance);
        return new BadgeEvaluationService(
            db,
            badgeService,
            publisher ?? new SpyAchievementEventPublisher(),
            NullLogger<BadgeEvaluationService>.Instance);
    }

    /// <summary>
    /// Records published events so tests can assert on event emission without a real DB.
    /// </summary>
    private sealed class SpyAchievementEventPublisher : IAchievementEventPublisher
    {
        public List<AchievementEventDto> Published { get; } = new();

        public Task PublishAsync(AchievementEventDto evt, CancellationToken ct = default)
        {
            Published.Add(evt);
            return Task.CompletedTask;
        }
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

    private static Badge SeedBadge(AppDbContext db, int id, BadgeType type, bool isActive = true)
    {
        var badge = new Badge
        {
            Id = id,
            Name = type.ToString(),
            Type = type,
            PointsReward = 10,
            IsActive = isActive
        };
        db.Badges.Add(badge);
        return badge;
    }

    /// <summary>
    /// Seeds a run with specific date and performance metrics.
    /// </summary>
    private static RunningActivity SeedRun(
        AppDbContext db,
        int id,
        int userId,
        DateOnly runDate,
        double distanceMeters = 5000,
        int movingTimeSeconds = 1500,
        DateTime? createdAt = null)
    {
        var run = new RunningActivity
        {
            Id = id,
            UserId = userId,
            ExternalActivityId = $"ext_{id}",
            Name = "Test Run",
            Type = "Run",
            StartTime = runDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            RunDate = runDate,
            DistanceMeters = distanceMeters,
            MovingTimeSeconds = movingTimeSeconds,
            ElapsedTimeSeconds = movingTimeSeconds,
            TotalElevationGain = 0,
            AverageSpeed = distanceMeters / movingTimeSeconds,
            Source = "STRAVA",
            CreatedAt = createdAt ?? runDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            UpdatedAt = runDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
        };
        db.RunningActivities.Add(run);
        return run;
    }

    private static void SeedOwnershipHistory(
        AppDbContext db,
        int id,
        int userId,
        OwnershipActionType actionType,
        int? previousOwnerUserId = null)
    {
        db.TerritoryOwnershipHistories.Add(new TerritoryOwnershipHistory
        {
            Id = id,
            TerritoryId = id,
            NewOwnerUserId = userId,
            PreviousOwnerUserId = previousOwnerUserId,
            ActionType = actionType,
            ActionRunId = id,
            ActionScore = 1.0m,
            ActionAt = DateTime.UtcNow
        });
    }

    private static void SeedCompletedChallenge(AppDbContext db, int id, int userId)
    {
        db.UserChallenges.Add(new UserChallenge
        {
            Id = id,
            UserId = userId,
            ChallengeId = id,
            JoinedAt = DateTime.UtcNow.AddDays(-30),
            Completed = true,
            CompletedAt = DateTime.UtcNow.AddDays(-1)
        });
    }

    private static void SeedUserBadge(AppDbContext db, int userId, int badgeId)
    {
        db.UserBadges.Add(new UserBadge
        {
            UserId = userId,
            BadgeId = badgeId,
            EarnedAtUtc = DateTime.UtcNow.AddDays(-1)
        });
    }

    // -------------------------------------------------------------------------
    // User not found
    // -------------------------------------------------------------------------

    [Fact]
    public async Task EvaluateBadgeConditions_WhenUserNotFound_ReturnsEmpty()
    {
        var db = CreateDb(nameof(EvaluateBadgeConditions_WhenUserNotFound_ReturnsEmpty));
        await db.SaveChangesAsync();

        var result = await CreateService(db).EvaluateBadgeConditionsAsync(999);

        Assert.NotNull(result);
        Assert.Empty(result.AwardedBadgeIds);
        Assert.False(result.AnyNewBadgeAwarded);
    }

    // -------------------------------------------------------------------------
    // First Run badge
    // -------------------------------------------------------------------------

    [Fact]
    public async Task EvaluateBadgeConditions_FirstRun_AwardedWhenUserHasRun()
    {
        var db = CreateDb(nameof(EvaluateBadgeConditions_FirstRun_AwardedWhenUserHasRun));
        SeedUser(db, 1);
        var badge = SeedBadge(db, 1, BadgeType.FirstRun);
        SeedRun(db, 1, userId: 1, runDate: new DateOnly(2025, 3, 1));
        await db.SaveChangesAsync();

        var result = await CreateService(db).EvaluateBadgeConditionsAsync(1);

        Assert.True(result.AnyNewBadgeAwarded);
        Assert.Contains(badge.Id, result.AwardedBadgeIds);
        var stored = await db.UserBadges.SingleOrDefaultAsync(ub => ub.UserId == 1 && ub.BadgeId == badge.Id);
        Assert.NotNull(stored);
    }

    [Fact]
    public async Task EvaluateBadgeConditions_FirstRun_NotAwardedWhenUserHasNoRuns()
    {
        var db = CreateDb(nameof(EvaluateBadgeConditions_FirstRun_NotAwardedWhenUserHasNoRuns));
        SeedUser(db, 1);
        SeedBadge(db, 1, BadgeType.FirstRun);
        await db.SaveChangesAsync();

        var result = await CreateService(db).EvaluateBadgeConditionsAsync(1);

        Assert.False(result.AnyNewBadgeAwarded);
        Assert.Empty(await db.UserBadges.ToListAsync());
    }

    [Fact]
    public async Task EvaluateBadgeConditions_FirstRun_NotReAwardedWhenAlreadyOwned()
    {
        var db = CreateDb(nameof(EvaluateBadgeConditions_FirstRun_NotReAwardedWhenAlreadyOwned));
        SeedUser(db, 1);
        var badge = SeedBadge(db, 1, BadgeType.FirstRun);
        SeedRun(db, 1, userId: 1, runDate: new DateOnly(2025, 3, 1));
        SeedUserBadge(db, userId: 1, badgeId: badge.Id);
        await db.SaveChangesAsync();

        var result = await CreateService(db).EvaluateBadgeConditionsAsync(1);

        Assert.False(result.AnyNewBadgeAwarded);
        Assert.Equal(1, result.AlreadyOwnedCount);
        var count = await db.UserBadges.CountAsync(ub => ub.UserId == 1 && ub.BadgeId == badge.Id);
        Assert.Equal(1, count);
    }

    // -------------------------------------------------------------------------
    // Weekly Streak badge
    // -------------------------------------------------------------------------

    [Fact]
    public async Task EvaluateBadgeConditions_Streak_AwardedWhenUserHasTwoConsecutiveWeeks()
    {
        var db = CreateDb(nameof(EvaluateBadgeConditions_Streak_AwardedWhenUserHasTwoConsecutiveWeeks));
        SeedUser(db, 1);
        var badge = SeedBadge(db, 2, BadgeType.Streak);
        // Two runs in consecutive ISO weeks.
        SeedRun(db, 1, userId: 1, runDate: new DateOnly(2025, 3, 3));  // Week 10, 2025
        SeedRun(db, 2, userId: 1, runDate: new DateOnly(2025, 3, 10)); // Week 11, 2025
        await db.SaveChangesAsync();

        var result = await CreateService(db).EvaluateBadgeConditionsAsync(1);

        Assert.Contains(badge.Id, result.AwardedBadgeIds);
    }

    [Fact]
    public async Task EvaluateBadgeConditions_Streak_NotAwardedWhenRunsInNonConsecutiveWeeks()
    {
        var db = CreateDb(nameof(EvaluateBadgeConditions_Streak_NotAwardedWhenRunsInNonConsecutiveWeeks));
        SeedUser(db, 1);
        SeedBadge(db, 2, BadgeType.Streak);
        // Gap of 2 weeks between runs — not consecutive.
        SeedRun(db, 1, userId: 1, runDate: new DateOnly(2025, 3, 3));  // Week 10
        SeedRun(db, 2, userId: 1, runDate: new DateOnly(2025, 3, 17)); // Week 12 (week 11 skipped)
        await db.SaveChangesAsync();

        var result = await CreateService(db).EvaluateBadgeConditionsAsync(1);

        Assert.DoesNotContain(2, result.AwardedBadgeIds);
    }

    [Fact]
    public async Task EvaluateBadgeConditions_Streak_NotAwardedWhenOnlyOneWeekOfRuns()
    {
        var db = CreateDb(nameof(EvaluateBadgeConditions_Streak_NotAwardedWhenOnlyOneWeekOfRuns));
        SeedUser(db, 1);
        SeedBadge(db, 2, BadgeType.Streak);
        // Two runs but both in the same ISO week.
        SeedRun(db, 1, userId: 1, runDate: new DateOnly(2025, 3, 3)); // Week 10
        SeedRun(db, 2, userId: 1, runDate: new DateOnly(2025, 3, 5)); // Week 10
        await db.SaveChangesAsync();

        var result = await CreateService(db).EvaluateBadgeConditionsAsync(1);

        Assert.DoesNotContain(2, result.AwardedBadgeIds);
    }

    // -------------------------------------------------------------------------
    // Personal Best badge
    // -------------------------------------------------------------------------

    [Fact]
    public async Task EvaluateBadgeConditions_PersonalBest_AwardedOnFirstQualifyingRun()
    {
        var db = CreateDb(nameof(EvaluateBadgeConditions_PersonalBest_AwardedOnFirstQualifyingRun));
        SeedUser(db, 1);
        var badge = SeedBadge(db, 3, BadgeType.PersonalBest);
        var now = DateTime.UtcNow;
        // One qualifying run (distance > 500 m).
        SeedRun(db, 1, userId: 1, runDate: new DateOnly(2025, 3, 1),
            distanceMeters: 5000, movingTimeSeconds: 1500, createdAt: now);
        await db.SaveChangesAsync();

        var result = await CreateService(db).EvaluateBadgeConditionsAsync(1);

        Assert.Contains(badge.Id, result.AwardedBadgeIds);
    }

    [Fact]
    public async Task EvaluateBadgeConditions_PersonalBest_AwardedWhenLatestRunIsFastest()
    {
        var db = CreateDb(nameof(EvaluateBadgeConditions_PersonalBest_AwardedWhenLatestRunIsFastest));
        SeedUser(db, 1);
        var badge = SeedBadge(db, 3, BadgeType.PersonalBest);
        var older = DateTime.UtcNow.AddDays(-2);
        var newer = DateTime.UtcNow;
        // Older run: pace = 1500/5000 = 0.3 s/m
        SeedRun(db, 1, userId: 1, runDate: new DateOnly(2025, 3, 1),
            distanceMeters: 5000, movingTimeSeconds: 1500, createdAt: older);
        // Newer run: pace = 1200/5000 = 0.24 s/m (faster)
        SeedRun(db, 2, userId: 1, runDate: new DateOnly(2025, 3, 3),
            distanceMeters: 5000, movingTimeSeconds: 1200, createdAt: newer);
        await db.SaveChangesAsync();

        var result = await CreateService(db).EvaluateBadgeConditionsAsync(1);

        Assert.Contains(badge.Id, result.AwardedBadgeIds);
    }

    [Fact]
    public async Task EvaluateBadgeConditions_PersonalBest_NotAwardedWhenLatestRunIsNotFastest()
    {
        var db = CreateDb(nameof(EvaluateBadgeConditions_PersonalBest_NotAwardedWhenLatestRunIsNotFastest));
        SeedUser(db, 1);
        SeedBadge(db, 3, BadgeType.PersonalBest);
        var older = DateTime.UtcNow.AddDays(-2);
        var newer = DateTime.UtcNow;
        // Older run: pace = 1200/5000 = 0.24 s/m (faster)
        SeedRun(db, 1, userId: 1, runDate: new DateOnly(2025, 3, 1),
            distanceMeters: 5000, movingTimeSeconds: 1200, createdAt: older);
        // Newer run: pace = 1800/5000 = 0.36 s/m (slower)
        SeedRun(db, 2, userId: 1, runDate: new DateOnly(2025, 3, 3),
            distanceMeters: 5000, movingTimeSeconds: 1800, createdAt: newer);
        await db.SaveChangesAsync();

        var result = await CreateService(db).EvaluateBadgeConditionsAsync(1);

        Assert.DoesNotContain(3, result.AwardedBadgeIds);
    }

    [Fact]
    public async Task EvaluateBadgeConditions_PersonalBest_NotAwardedWhenRunTooShort()
    {
        var db = CreateDb(nameof(EvaluateBadgeConditions_PersonalBest_NotAwardedWhenRunTooShort));
        SeedUser(db, 1);
        SeedBadge(db, 3, BadgeType.PersonalBest);
        // Run below the 500 m qualifying threshold.
        SeedRun(db, 1, userId: 1, runDate: new DateOnly(2025, 3, 1), distanceMeters: 400);
        await db.SaveChangesAsync();

        var result = await CreateService(db).EvaluateBadgeConditionsAsync(1);

        Assert.DoesNotContain(3, result.AwardedBadgeIds);
    }

    // -------------------------------------------------------------------------
    // Territory badge — First Claim
    // -------------------------------------------------------------------------

    [Fact]
    public async Task EvaluateBadgeConditions_TerritoryFirstClaim_AwardedWhenUserHasClaimHistory()
    {
        var db = CreateDb(nameof(EvaluateBadgeConditions_TerritoryFirstClaim_AwardedWhenUserHasClaimHistory));
        SeedUser(db, 1);
        var badge = SeedBadge(db, 6, BadgeType.TerritoryFirstClaim);
        SeedOwnershipHistory(db, id: 1, userId: 1, actionType: OwnershipActionType.Claim);
        await db.SaveChangesAsync();

        var result = await CreateService(db).EvaluateBadgeConditionsAsync(1);

        Assert.Contains(badge.Id, result.AwardedBadgeIds);
    }

    [Fact]
    public async Task EvaluateBadgeConditions_TerritoryFirstClaim_NotAwardedWhenUserHasNoClaimHistory()
    {
        var db = CreateDb(nameof(EvaluateBadgeConditions_TerritoryFirstClaim_NotAwardedWhenUserHasNoClaimHistory));
        SeedUser(db, 1);
        SeedBadge(db, 6, BadgeType.TerritoryFirstClaim);
        await db.SaveChangesAsync();

        var result = await CreateService(db).EvaluateBadgeConditionsAsync(1);

        Assert.DoesNotContain(6, result.AwardedBadgeIds);
    }

    // -------------------------------------------------------------------------
    // Territory badge — Defender
    // -------------------------------------------------------------------------

    [Fact]
    public async Task EvaluateBadgeConditions_TerritoryDefender_AwardedWhenUserHasDefendHistory()
    {
        var db = CreateDb(nameof(EvaluateBadgeConditions_TerritoryDefender_AwardedWhenUserHasDefendHistory));
        SeedUser(db, 1);
        var badge = SeedBadge(db, 7, BadgeType.TerritoryDefender);
        SeedOwnershipHistory(db, id: 1, userId: 1, actionType: OwnershipActionType.Defend);
        await db.SaveChangesAsync();

        var result = await CreateService(db).EvaluateBadgeConditionsAsync(1);

        Assert.Contains(badge.Id, result.AwardedBadgeIds);
    }

    [Fact]
    public async Task EvaluateBadgeConditions_TerritoryDefender_NotAwardedWhenUserHasNoDefendHistory()
    {
        var db = CreateDb(nameof(EvaluateBadgeConditions_TerritoryDefender_NotAwardedWhenUserHasNoDefendHistory));
        SeedUser(db, 1);
        SeedBadge(db, 7, BadgeType.TerritoryDefender);
        SeedOwnershipHistory(db, id: 1, userId: 1, actionType: OwnershipActionType.Claim);
        await db.SaveChangesAsync();

        var result = await CreateService(db).EvaluateBadgeConditionsAsync(1);

        Assert.DoesNotContain(7, result.AwardedBadgeIds);
    }

    // -------------------------------------------------------------------------
    // Territory badge — Conqueror
    // -------------------------------------------------------------------------

    [Fact]
    public async Task EvaluateBadgeConditions_TerritoryConqueror_AwardedWhenUserHasTransferHistory()
    {
        var db = CreateDb(nameof(EvaluateBadgeConditions_TerritoryConqueror_AwardedWhenUserHasTransferHistory));
        SeedUser(db, 1);
        SeedUser(db, 2); // previous owner
        var badge = SeedBadge(db, 8, BadgeType.TerritoryConqueror);
        // Transfer: user 1 took territory from user 2
        SeedOwnershipHistory(db, id: 1, userId: 1, actionType: OwnershipActionType.Transfer, previousOwnerUserId: 2);
        await db.SaveChangesAsync();

        var result = await CreateService(db).EvaluateBadgeConditionsAsync(1);

        Assert.Contains(badge.Id, result.AwardedBadgeIds);
    }

    [Fact]
    public async Task EvaluateBadgeConditions_TerritoryConqueror_NotAwardedWhenUserHasOnlyClaims()
    {
        var db = CreateDb(nameof(EvaluateBadgeConditions_TerritoryConqueror_NotAwardedWhenUserHasOnlyClaims));
        SeedUser(db, 1);
        SeedBadge(db, 8, BadgeType.TerritoryConqueror);
        SeedOwnershipHistory(db, id: 1, userId: 1, actionType: OwnershipActionType.Claim);
        await db.SaveChangesAsync();

        var result = await CreateService(db).EvaluateBadgeConditionsAsync(1);

        Assert.DoesNotContain(8, result.AwardedBadgeIds);
    }

    // -------------------------------------------------------------------------
    // Challenge Completion badge
    // -------------------------------------------------------------------------

    [Fact]
    public async Task EvaluateBadgeConditions_ChallengeCompletion_AwardedWhenUserHasCompletedChallenge()
    {
        var db = CreateDb(nameof(EvaluateBadgeConditions_ChallengeCompletion_AwardedWhenUserHasCompletedChallenge));
        SeedUser(db, 1);
        var badge = SeedBadge(db, 5, BadgeType.ChallengeCompletion);
        SeedCompletedChallenge(db, id: 1, userId: 1);
        await db.SaveChangesAsync();

        var result = await CreateService(db).EvaluateBadgeConditionsAsync(1);

        Assert.Contains(badge.Id, result.AwardedBadgeIds);
    }

    [Fact]
    public async Task EvaluateBadgeConditions_ChallengeCompletion_NotAwardedWhenChallengeNotCompleted()
    {
        var db = CreateDb(nameof(EvaluateBadgeConditions_ChallengeCompletion_NotAwardedWhenChallengeNotCompleted));
        SeedUser(db, 1);
        SeedBadge(db, 5, BadgeType.ChallengeCompletion);
        db.UserChallenges.Add(new UserChallenge
        {
            Id = 1,
            UserId = 1,
            ChallengeId = 1,
            JoinedAt = DateTime.UtcNow.AddDays(-10),
            Completed = false
        });
        await db.SaveChangesAsync();

        var result = await CreateService(db).EvaluateBadgeConditionsAsync(1);

        Assert.DoesNotContain(5, result.AwardedBadgeIds);
    }

    // -------------------------------------------------------------------------
    // Idempotency — repeated evaluation never creates duplicates
    // -------------------------------------------------------------------------

    [Fact]
    public async Task EvaluateBadgeConditions_CalledTwice_DoesNotCreateDuplicateBadges()
    {
        var db = CreateDb(nameof(EvaluateBadgeConditions_CalledTwice_DoesNotCreateDuplicateBadges));
        SeedUser(db, 1);
        SeedBadge(db, 1, BadgeType.FirstRun);
        SeedRun(db, 1, userId: 1, runDate: new DateOnly(2025, 3, 1));
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var first = await service.EvaluateBadgeConditionsAsync(1);
        var second = await service.EvaluateBadgeConditionsAsync(1);

        Assert.True(first.AnyNewBadgeAwarded);
        Assert.False(second.AnyNewBadgeAwarded);
        Assert.Equal(1, second.AlreadyOwnedCount);
        var count = await db.UserBadges.CountAsync(ub => ub.UserId == 1 && ub.BadgeId == 1);
        Assert.Equal(1, count);
    }

    // -------------------------------------------------------------------------
    // No active badge for type — gracefully skipped
    // -------------------------------------------------------------------------

    [Fact]
    public async Task EvaluateBadgeConditions_NoBadgeDefinedForType_SkipsGracefully()
    {
        var db = CreateDb(nameof(EvaluateBadgeConditions_NoBadgeDefinedForType_SkipsGracefully));
        SeedUser(db, 1);
        // Seed a run but do NOT seed a FirstRun badge row.
        SeedRun(db, 1, userId: 1, runDate: new DateOnly(2025, 3, 1));
        await db.SaveChangesAsync();

        // Should not throw even though the badge row is missing.
        var result = await CreateService(db).EvaluateBadgeConditionsAsync(1);

        Assert.False(result.AnyNewBadgeAwarded);
        Assert.Empty(await db.UserBadges.ToListAsync());
    }

    // -------------------------------------------------------------------------
    // Multiple badges awarded in single evaluation
    // -------------------------------------------------------------------------

    [Fact]
    public async Task EvaluateBadgeConditions_MultipleConditionsMet_AwardsAllEligibleBadges()
    {
        var db = CreateDb(nameof(EvaluateBadgeConditions_MultipleConditionsMet_AwardsAllEligibleBadges));
        SeedUser(db, 1);
        var firstRunBadge = SeedBadge(db, 1, BadgeType.FirstRun);
        var claimBadge = SeedBadge(db, 6, BadgeType.TerritoryFirstClaim);
        var completionBadge = SeedBadge(db, 5, BadgeType.ChallengeCompletion);

        SeedRun(db, 1, userId: 1, runDate: new DateOnly(2025, 3, 1));
        SeedOwnershipHistory(db, id: 1, userId: 1, actionType: OwnershipActionType.Claim);
        SeedCompletedChallenge(db, id: 1, userId: 1);
        await db.SaveChangesAsync();

        var result = await CreateService(db).EvaluateBadgeConditionsAsync(1);

        Assert.Contains(firstRunBadge.Id, result.AwardedBadgeIds);
        Assert.Contains(claimBadge.Id, result.AwardedBadgeIds);
        Assert.Contains(completionBadge.Id, result.AwardedBadgeIds);
        Assert.Equal(3, result.AwardedBadgeIds.Count);
    }

    // -------------------------------------------------------------------------
    // Territory badge — only awarded to the correct user
    // -------------------------------------------------------------------------

    [Fact]
    public async Task EvaluateBadgeConditions_TerritoryFirstClaim_NotAwardedToOtherUser()
    {
        var db = CreateDb(nameof(EvaluateBadgeConditions_TerritoryFirstClaim_NotAwardedToOtherUser));
        SeedUser(db, 1);
        SeedUser(db, 2);
        SeedBadge(db, 6, BadgeType.TerritoryFirstClaim);
        // History belongs to user 2, not user 1.
        SeedOwnershipHistory(db, id: 1, userId: 2, actionType: OwnershipActionType.Claim);
        await db.SaveChangesAsync();

        var result = await CreateService(db).EvaluateBadgeConditionsAsync(1);

        Assert.DoesNotContain(6, result.AwardedBadgeIds);
    }

    // -------------------------------------------------------------------------
    // BE-5 event emission — BadgeEarned
    // -------------------------------------------------------------------------

    [Fact]
    public async Task EvaluateBadgeConditions_FirstRun_EmitsBadgeEarnedEventOnce()
    {
        var db = CreateDb(nameof(EvaluateBadgeConditions_FirstRun_EmitsBadgeEarnedEventOnce));
        SeedUser(db, 1);
        var badge = SeedBadge(db, 1, BadgeType.FirstRun);
        SeedRun(db, 1, userId: 1, runDate: new DateOnly(2025, 3, 1));
        await db.SaveChangesAsync();

        var spy = new SpyAchievementEventPublisher();
        await CreateService(db, spy).EvaluateBadgeConditionsAsync(1);

        var badgeEvents = spy.Published
            .Where(e => e.Type == AchievementEventType.BadgeEarned && e.BadgeId == badge.Id)
            .ToList();
        Assert.Single(badgeEvents);
        Assert.Equal(1, badgeEvents[0].UserId);
        Assert.Equal(AchievementDeduplicationKeys.BadgeEarned(1, badge.Id), badgeEvents[0].DeduplicationKey);
    }

    [Fact]
    public async Task EvaluateBadgeConditions_FirstRun_NoBadgeEarnedEventWhenAlreadyOwned()
    {
        var db = CreateDb(nameof(EvaluateBadgeConditions_FirstRun_NoBadgeEarnedEventWhenAlreadyOwned));
        SeedUser(db, 1);
        var badge = SeedBadge(db, 1, BadgeType.FirstRun);
        SeedRun(db, 1, userId: 1, runDate: new DateOnly(2025, 3, 1));
        SeedUserBadge(db, userId: 1, badgeId: badge.Id);
        await db.SaveChangesAsync();

        var spy = new SpyAchievementEventPublisher();
        await CreateService(db, spy).EvaluateBadgeConditionsAsync(1);

        Assert.DoesNotContain(spy.Published, e => e.Type == AchievementEventType.BadgeEarned);
    }

    // -------------------------------------------------------------------------
    // BE-5 event emission — PersonalBest
    // -------------------------------------------------------------------------

    [Fact]
    public async Task EvaluateBadgeConditions_PersonalBest_EmitsBothBadgeEarnedAndPersonalBestEvents()
    {
        var db = CreateDb(nameof(EvaluateBadgeConditions_PersonalBest_EmitsBothBadgeEarnedAndPersonalBestEvents));
        SeedUser(db, 1);
        var badge = SeedBadge(db, 3, BadgeType.PersonalBest);
        var now = DateTime.UtcNow;
        SeedRun(db, 42, userId: 1, runDate: new DateOnly(2025, 3, 1),
            distanceMeters: 5000, movingTimeSeconds: 1200, createdAt: now);
        await db.SaveChangesAsync();

        var spy = new SpyAchievementEventPublisher();
        await CreateService(db, spy).EvaluateBadgeConditionsAsync(1);

        var badgeEvent = spy.Published.Single(e => e.Type == AchievementEventType.BadgeEarned);
        Assert.Equal(badge.Id, badgeEvent.BadgeId);
        Assert.Equal(42, badgeEvent.RunId);

        var pbEvent = spy.Published.Single(e => e.Type == AchievementEventType.PersonalBest);
        Assert.Equal(42, pbEvent.RunId);
        Assert.Equal(AchievementDeduplicationKeys.PersonalBest(1, 42), pbEvent.DeduplicationKey);
    }

    [Fact]
    public async Task EvaluateBadgeConditions_PersonalBest_NoEventsEmittedWhenBadgeAlreadyOwned()
    {
        var db = CreateDb(nameof(EvaluateBadgeConditions_PersonalBest_NoEventsEmittedWhenBadgeAlreadyOwned));
        SeedUser(db, 1);
        var badge = SeedBadge(db, 3, BadgeType.PersonalBest);
        var now = DateTime.UtcNow;
        SeedRun(db, 42, userId: 1, runDate: new DateOnly(2025, 3, 1),
            distanceMeters: 5000, movingTimeSeconds: 1200, createdAt: now);
        SeedUserBadge(db, userId: 1, badgeId: badge.Id);
        await db.SaveChangesAsync();

        var spy = new SpyAchievementEventPublisher();
        await CreateService(db, spy).EvaluateBadgeConditionsAsync(1);

        Assert.DoesNotContain(spy.Published, e =>
            e.Type == AchievementEventType.PersonalBest || e.Type == AchievementEventType.BadgeEarned);
    }

    [Fact]
    public async Task EvaluateBadgeConditions_CalledTwice_EmitsBadgeEarnedOnlyOnce()
    {
        var db = CreateDb(nameof(EvaluateBadgeConditions_CalledTwice_EmitsBadgeEarnedOnlyOnce));
        SeedUser(db, 1);
        SeedBadge(db, 1, BadgeType.FirstRun);
        SeedRun(db, 1, userId: 1, runDate: new DateOnly(2025, 3, 1));
        await db.SaveChangesAsync();

        var spy = new SpyAchievementEventPublisher();
        var service = CreateService(db, spy);
        await service.EvaluateBadgeConditionsAsync(1);
        await service.EvaluateBadgeConditionsAsync(1);

        // Badge already owned on second call → no second event.
        Assert.Single(spy.Published, e => e.Type == AchievementEventType.BadgeEarned);
    }
}
