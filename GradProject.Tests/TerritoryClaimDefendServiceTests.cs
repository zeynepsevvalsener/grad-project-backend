using GradProject.Application.DTOs.Gamification;
using GradProject.Application.Interfaces;
using GradProject.Application.Interfaces.Gamification;
using GradProject.Application.Models;
using GradProject.Domain.Entities;
using GradProject.Domain.Enums;
using GradProject.Infrastructure.Persistence;
using GradProject.Infrastructure.Services.Gamification;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace GradProject.Tests;

public class TerritoryClaimDefendServiceTests
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

    private static TerritoryClaimDefendService CreateService(
        AppDbContext db,
        ITerritoryScoreEngine engine,
        IAchievementEventPublisher? publisher = null)
    {
        var badgeService = new BadgeService(db, NullLogger<BadgeService>.Instance);
        var badgeEvalService = new BadgeEvaluationService(db, badgeService, NullLogger<BadgeEvaluationService>.Instance);
        return new TerritoryClaimDefendService(
            db,
            engine,
            badgeEvalService,
            NullLogger<TerritoryClaimDefendService>.Instance);
    }

    private sealed class NoOpAchievementEventPublisher : IAchievementEventPublisher
    {
        public Task PublishAsync(AchievementEventDto evt, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class SpyAchievementEventPublisher : IAchievementEventPublisher
    {
        public List<AchievementEventDto> Published { get; } = new();
        public Task PublishAsync(AchievementEventDto evt, CancellationToken ct = default)
        {
            Published.Add(evt);
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task Claim_WhenTerritoryEmpty_ClaimsSuccessfully()
    {
        var db = CreateDb(nameof(Claim_WhenTerritoryEmpty_ClaimsSuccessfully));
        var user = new User { Id = 1, Email = "u@x.com", PasswordHash = Array.Empty<byte>(), PasswordSalt = Array.Empty<byte>() };
        db.Users.Add(user);
        db.RunningActivities.Add(new RunningActivity
        {
            Id = 10,
            UserId = 1,
            ExternalActivityId = "ext",
            Name = "Run",
            Type = "Run",
            StartTime = DateTime.UtcNow,
            RunDate = DateOnly.FromDateTime(DateTime.UtcNow),
            DistanceMeters = 5000,
            MovingTimeSeconds = 1200,
            ElapsedTimeSeconds = 1200,
            TotalElevationGain = 0,
            AverageSpeed = 5000.0 / 1200,
            Source = "STRAVA",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        db.Territories.Add(new Territory
        {
            Id = 1,
            Name = "T1",
            IsActive = true,
            CurrentOwnerUserId = null,
            CurrentOwnerScoreSnapshot = null,
            Version = 0
        });
        await db.SaveChangesAsync();

        var engine = new StubTerritoryScoreEngine(new[]
        {
            new TerritoryContribution { TerritoryId = 1, FinalScore = 0.5, CoverageRatio = 0.5, DistanceInTerritory = 2500 }
        });
        var service = CreateService(db, engine);

        var result = await service.ClaimAsync(1, 10, new[] { 1 });

        Assert.Single(result.ClaimedTerritories, 1);
        Assert.Empty(result.RejectedTerritories);
        Assert.Single(result.UpdatedOwnership);
        Assert.Equal(1, result.UpdatedOwnership[0].TerritoryId);
        Assert.Equal(1, result.UpdatedOwnership[0].NewOwnerUserId);
        Assert.Null(result.UpdatedOwnership[0].PreviousOwnerUserId);
        Assert.Equal("Claim", result.UpdatedOwnership[0].ActionType);
        Assert.Single(result.EventsToEmit);
        Assert.Equal("TERRITORY_CLAIMED", result.EventsToEmit[0].EventType);

        var territory = await db.Territories.FindAsync(1);
        Assert.NotNull(territory);
        Assert.Equal(1, territory.CurrentOwnerUserId);
        Assert.NotNull(territory.CurrentOwnerScoreSnapshot);
        var history = await db.TerritoryOwnershipHistories.Where(h => h.TerritoryId == 1 && h.ActionRunId == 10).ToListAsync();
        Assert.Single(history);
    }

    [Fact]
    public async Task Claim_WhenTerritoryOccupiedAndScoreGreater_TransfersOwnership()
    {
        var db = CreateDb(nameof(Claim_WhenTerritoryOccupiedAndScoreGreater_TransfersOwnership));
        db.Users.Add(new User { Id = 1, Email = "u1@x.com", PasswordHash = Array.Empty<byte>(), PasswordSalt = Array.Empty<byte>() });
        db.Users.Add(new User { Id = 2, Email = "u2@x.com", PasswordHash = Array.Empty<byte>(), PasswordSalt = Array.Empty<byte>() });
        db.RunningActivities.Add(new RunningActivity
        {
            Id = 20,
            UserId = 2,
            ExternalActivityId = "ext2",
            Name = "Run2",
            Type = "Run",
            StartTime = DateTime.UtcNow,
            RunDate = DateOnly.FromDateTime(DateTime.UtcNow),
            DistanceMeters = 10000,
            MovingTimeSeconds = 2400,
            ElapsedTimeSeconds = 2400,
            TotalElevationGain = 0,
            AverageSpeed = 10000.0 / 2400,
            Source = "STRAVA",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        db.Territories.Add(new Territory
        {
            Id = 2,
            Name = "T2",
            IsActive = true,
            CurrentOwnerUserId = 1,
            CurrentOwnerSince = DateTime.UtcNow.AddDays(-1),
            CurrentOwnerScoreSnapshot = 0.3m,
            Version = 0
        });
        await db.SaveChangesAsync();

        var engine = new StubTerritoryScoreEngine(new[]
        {
            new TerritoryContribution { TerritoryId = 2, FinalScore = 0.6, CoverageRatio = 0.8, DistanceInTerritory = 8000 }
        });
        var service = CreateService(db, engine);

        var result = await service.ClaimAsync(2, 20, new[] { 2 });

        Assert.Single(result.ClaimedTerritories, 2);
        Assert.Single(result.UpdatedOwnership);
        Assert.Equal(2, result.UpdatedOwnership[0].NewOwnerUserId);
        Assert.Equal(1, result.UpdatedOwnership[0].PreviousOwnerUserId);
        Assert.Equal("Transfer", result.UpdatedOwnership[0].ActionType);
        Assert.Equal(2, result.EventsToEmit.Count);
        Assert.Contains(result.EventsToEmit, e => e.EventType == "TERRITORY_TRANSFERRED" && e.UserId == 2);
        Assert.Contains(result.EventsToEmit, e => e.EventType == "TERRITORY_LOST" && e.UserId == 1);

        var territory = await db.Territories.FindAsync(2);
        Assert.Equal(2, territory!.CurrentOwnerUserId);
        Assert.Equal(0.6m, territory.CurrentOwnerScoreSnapshot);
    }

    [Fact]
    public async Task Claim_WhenTerritoryOccupiedAndScoreNotGreater_RejectsWithInsufficientScore()
    {
        var db = CreateDb(nameof(Claim_WhenTerritoryOccupiedAndScoreNotGreater_RejectsWithInsufficientScore));
        db.Users.Add(new User { Id = 1, Email = "u@x.com", PasswordHash = Array.Empty<byte>(), PasswordSalt = Array.Empty<byte>() });
        db.RunningActivities.Add(new RunningActivity
        {
            Id = 30,
            UserId = 1,
            ExternalActivityId = "ext",
            Name = "Run",
            Type = "Run",
            StartTime = DateTime.UtcNow,
            RunDate = DateOnly.FromDateTime(DateTime.UtcNow),
            DistanceMeters = 1000,
            MovingTimeSeconds = 300,
            ElapsedTimeSeconds = 300,
            TotalElevationGain = 0,
            AverageSpeed = 1000.0 / 300,
            Source = "STRAVA",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        db.Territories.Add(new Territory
        {
            Id = 3,
            Name = "T3",
            IsActive = true,
            CurrentOwnerUserId = 2,
            CurrentOwnerSince = DateTime.UtcNow,
            CurrentOwnerScoreSnapshot = 0.8m,
            Version = 0
        });
        db.Users.Add(new User { Id = 2, Email = "owner@x.com", PasswordHash = Array.Empty<byte>(), PasswordSalt = Array.Empty<byte>() });
        await db.SaveChangesAsync();

        var engine = new StubTerritoryScoreEngine(new[]
        {
            new TerritoryContribution { TerritoryId = 3, FinalScore = 0.5, CoverageRatio = 0.3, DistanceInTerritory = 300 }
        });
        var service = CreateService(db, engine);

        var result = await service.ClaimAsync(1, 30, new[] { 3 });

        Assert.Empty(result.ClaimedTerritories);
        Assert.Single(result.RejectedTerritories);
        Assert.Equal(3, result.RejectedTerritories[0].TerritoryId);
        Assert.Equal("INSUFFICIENT_SCORE", result.RejectedTerritories[0].Reason);
        Assert.Empty(result.UpdatedOwnership);

        var territory = await db.Territories.FindAsync(3);
        Assert.Equal(2, territory!.CurrentOwnerUserId);
    }

    [Fact]
    public async Task Defend_WhenOwner_UpdatesSnapshot()
    {
        var db = CreateDb(nameof(Defend_WhenOwner_UpdatesSnapshot));
        db.Users.Add(new User { Id = 1, Email = "u@x.com", PasswordHash = Array.Empty<byte>(), PasswordSalt = Array.Empty<byte>() });
        db.RunningActivities.Add(new RunningActivity
        {
            Id = 40,
            UserId = 1,
            ExternalActivityId = "ext",
            Name = "Run",
            Type = "Run",
            StartTime = DateTime.UtcNow,
            RunDate = DateOnly.FromDateTime(DateTime.UtcNow),
            DistanceMeters = 8000,
            MovingTimeSeconds = 2000,
            ElapsedTimeSeconds = 2000,
            TotalElevationGain = 0,
            AverageSpeed = 4,
            Source = "STRAVA",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        db.Territories.Add(new Territory
        {
            Id = 4,
            Name = "T4",
            IsActive = true,
            CurrentOwnerUserId = 1,
            CurrentOwnerSince = DateTime.UtcNow.AddDays(-1),
            CurrentOwnerScoreSnapshot = 0.4m,
            Version = 0
        });
        await db.SaveChangesAsync();

        var engine = new StubTerritoryScoreEngine(new[]
        {
            new TerritoryContribution { TerritoryId = 4, FinalScore = 0.7, CoverageRatio = 0.9, DistanceInTerritory = 7200 }
        });
        var service = CreateService(db, engine);

        var result = await service.DefendAsync(1, 40, new[] { 4 });

        Assert.Single(result.DefendedTerritories, 4);
        Assert.Empty(result.RejectedTerritories);
        Assert.Single(result.UpdatedScoreSnapshots);
        Assert.Equal(0.4, result.UpdatedScoreSnapshots[0].OldScore);
        Assert.Equal(0.7, result.UpdatedScoreSnapshots[0].NewScore);
        Assert.Single(result.EventsToEmit);
        Assert.Equal("TERRITORY_DEFENDED", result.EventsToEmit[0].EventType);

        var territory = await db.Territories.FindAsync(4);
        Assert.Equal(0.7m, territory!.CurrentOwnerScoreSnapshot);
    }

    [Fact]
    public async Task Defend_WhenNotOwner_RejectsWithNotOwner()
    {
        var db = CreateDb(nameof(Defend_WhenNotOwner_RejectsWithNotOwner));
        db.Users.Add(new User { Id = 1, Email = "u1@x.com", PasswordHash = Array.Empty<byte>(), PasswordSalt = Array.Empty<byte>() });
        db.Users.Add(new User { Id = 2, Email = "u2@x.com", PasswordHash = Array.Empty<byte>(), PasswordSalt = Array.Empty<byte>() });
        db.RunningActivities.Add(new RunningActivity
        {
            Id = 50,
            UserId = 1,
            ExternalActivityId = "ext",
            Name = "Run",
            Type = "Run",
            StartTime = DateTime.UtcNow,
            RunDate = DateOnly.FromDateTime(DateTime.UtcNow),
            DistanceMeters = 5000,
            MovingTimeSeconds = 1200,
            ElapsedTimeSeconds = 1200,
            TotalElevationGain = 0,
            AverageSpeed = 5000.0 / 1200,
            Source = "STRAVA",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        db.Territories.Add(new Territory
        {
            Id = 5,
            Name = "T5",
            IsActive = true,
            CurrentOwnerUserId = 2,
            CurrentOwnerSince = DateTime.UtcNow,
            CurrentOwnerScoreSnapshot = 0.5m,
            Version = 0
        });
        await db.SaveChangesAsync();

        var engine = new StubTerritoryScoreEngine(new[]
        {
            new TerritoryContribution { TerritoryId = 5, FinalScore = 0.6, CoverageRatio = 0.5, DistanceInTerritory = 2500 }
        });
        var service = CreateService(db, engine);

        var result = await service.DefendAsync(1, 50, new[] { 5 });

        Assert.Empty(result.DefendedTerritories);
        Assert.Single(result.RejectedTerritories);
        Assert.Equal(5, result.RejectedTerritories[0].TerritoryId);
        Assert.Equal("NOT_OWNER", result.RejectedTerritories[0].Reason);
        Assert.Empty(result.UpdatedScoreSnapshots);

        var territory = await db.Territories.FindAsync(5);
        Assert.Equal(0.5m, territory!.CurrentOwnerScoreSnapshot);
    }

    [Fact]
    public async Task Claim_Idempotency_SameRunAndTerritory_DoesNotDuplicateHistory()
    {
        var db = CreateDb(nameof(Claim_Idempotency_SameRunAndTerritory_DoesNotDuplicateHistory));
        db.Users.Add(new User { Id = 1, Email = "u@x.com", PasswordHash = Array.Empty<byte>(), PasswordSalt = Array.Empty<byte>() });
        db.RunningActivities.Add(new RunningActivity
        {
            Id = 60,
            UserId = 1,
            ExternalActivityId = "ext",
            Name = "Run",
            Type = "Run",
            StartTime = DateTime.UtcNow,
            RunDate = DateOnly.FromDateTime(DateTime.UtcNow),
            DistanceMeters = 5000,
            MovingTimeSeconds = 1200,
            ElapsedTimeSeconds = 1200,
            TotalElevationGain = 0,
            AverageSpeed = 5000.0 / 1200,
            Source = "STRAVA",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        db.Territories.Add(new Territory
        {
            Id = 6,
            Name = "T6",
            IsActive = true,
            CurrentOwnerUserId = null,
            Version = 0
        });
        await db.SaveChangesAsync();

        var engine = new StubTerritoryScoreEngine(new[]
        {
            new TerritoryContribution { TerritoryId = 6, FinalScore = 0.5, CoverageRatio = 0.5, DistanceInTerritory = 2500 }
        });
        var service = CreateService(db, engine);

        var first = await service.ClaimAsync(1, 60, new[] { 6 });
        var second = await service.ClaimAsync(1, 60, new[] { 6 });

        Assert.Single(first.ClaimedTerritories);
        Assert.Single(second.ClaimedTerritories);
        var historyCount = await db.TerritoryOwnershipHistories.CountAsync(h => h.TerritoryId == 6 && h.ActionRunId == 60);
        Assert.Equal(1, historyCount);
    }

    [Fact]
    public async Task Claim_WhenRunNotOwnedByUser_Throws()
    {
        var db = CreateDb(nameof(Claim_WhenRunNotOwnedByUser_Throws));
        db.Users.Add(new User { Id = 1, Email = "u1@x.com", PasswordHash = Array.Empty<byte>(), PasswordSalt = Array.Empty<byte>() });
        db.Users.Add(new User { Id = 2, Email = "u2@x.com", PasswordHash = Array.Empty<byte>(), PasswordSalt = Array.Empty<byte>() });
        db.RunningActivities.Add(new RunningActivity
        {
            Id = 70,
            UserId = 2,
            ExternalActivityId = "ext",
            Name = "Run",
            Type = "Run",
            StartTime = DateTime.UtcNow,
            RunDate = DateOnly.FromDateTime(DateTime.UtcNow),
            DistanceMeters = 5000,
            MovingTimeSeconds = 1200,
            ElapsedTimeSeconds = 1200,
            TotalElevationGain = 0,
            AverageSpeed = 4,
            Source = "STRAVA",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        db.Territories.Add(new Territory { Id = 7, Name = "T7", IsActive = true, Version = 0 });
        await db.SaveChangesAsync();

        var engine = new StubTerritoryScoreEngine(new[]
        {
            new TerritoryContribution { TerritoryId = 7, FinalScore = 0.5, CoverageRatio = 0.5, DistanceInTerritory = 2500 }
        });
        var service = CreateService(db, engine);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.ClaimAsync(1, 70, new[] { 7 }));
    }

    // -------------------------------------------------------------------------
    // BE-5 — territory achievement events
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Claim_EmitsTerritoryClaimed_AchievementEvent()
    {
        var db = CreateDb(nameof(Claim_EmitsTerritoryClaimed_AchievementEvent));
        db.Users.Add(new User { Id = 1, Email = "u@x.com", PasswordHash = Array.Empty<byte>(), PasswordSalt = Array.Empty<byte>() });
        db.RunningActivities.Add(new RunningActivity
        {
            Id = 80, UserId = 1, ExternalActivityId = "ext80", Name = "R", Type = "Run",
            StartTime = DateTime.UtcNow, RunDate = DateOnly.FromDateTime(DateTime.UtcNow),
            DistanceMeters = 5000, MovingTimeSeconds = 1200, ElapsedTimeSeconds = 1200,
            TotalElevationGain = 0, AverageSpeed = 4, Source = "STRAVA",
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        db.Territories.Add(new Territory { Id = 80, Name = "T80", IsActive = true, Version = 0 });
        await db.SaveChangesAsync();

        var spy = new SpyAchievementEventPublisher();
        var engine = new StubTerritoryScoreEngine(new[]
        {
            new TerritoryContribution { TerritoryId = 80, FinalScore = 0.5, CoverageRatio = 0.5, DistanceInTerritory = 2500 }
        });
        var service = CreateService(db, engine, spy);

        await service.ClaimAsync(1, 80, new[] { 80 });

        var claimEvents = spy.Published
            .Where(e => e.Type == AchievementEventType.TerritoryClaimed)
            .ToList();
        Assert.Single(claimEvents);
        Assert.Equal(1, claimEvents[0].UserId);
        Assert.Equal(80, claimEvents[0].TerritoryId);
        Assert.Equal(AchievementDeduplicationKeys.TerritoryClaimed(1, 80, 80), claimEvents[0].DeduplicationKey);
    }

    [Fact]
    public async Task Claim_Transfer_EmitsTerritoryClaimedForNewOwnerAndTerritoryLostForPreviousOwner()
    {
        var db = CreateDb(nameof(Claim_Transfer_EmitsTerritoryClaimedForNewOwnerAndTerritoryLostForPreviousOwner));
        db.Users.Add(new User { Id = 1, Email = "u1@x.com", PasswordHash = Array.Empty<byte>(), PasswordSalt = Array.Empty<byte>() });
        db.Users.Add(new User { Id = 2, Email = "u2@x.com", PasswordHash = Array.Empty<byte>(), PasswordSalt = Array.Empty<byte>() });
        db.RunningActivities.Add(new RunningActivity
        {
            Id = 90, UserId = 1, ExternalActivityId = "ext90", Name = "R", Type = "Run",
            StartTime = DateTime.UtcNow, RunDate = DateOnly.FromDateTime(DateTime.UtcNow),
            DistanceMeters = 5000, MovingTimeSeconds = 1200, ElapsedTimeSeconds = 1200,
            TotalElevationGain = 0, AverageSpeed = 4, Source = "STRAVA",
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        // Territory already owned by user 2 with a lower score.
        db.Territories.Add(new Territory
        {
            Id = 90, Name = "T90", IsActive = true,
            CurrentOwnerUserId = 2, CurrentOwnerScoreSnapshot = 0.3m, Version = 0
        });
        await db.SaveChangesAsync();

        var spy = new SpyAchievementEventPublisher();
        // User 1 submits a higher score and takes the territory.
        var engine = new StubTerritoryScoreEngine(new[]
        {
            new TerritoryContribution { TerritoryId = 90, FinalScore = 0.8, CoverageRatio = 0.8, DistanceInTerritory = 4000 }
        });
        var service = CreateService(db, engine, spy);

        await service.ClaimAsync(1, 90, new[] { 90 });

        // New owner gets TerritoryClaimed.
        var claimedForUser1 = spy.Published
            .Where(e => e.Type == AchievementEventType.TerritoryClaimed && e.UserId == 1)
            .ToList();
        Assert.Single(claimedForUser1);

        // Previous owner gets TerritoryLost.
        var lostForUser2 = spy.Published
            .Where(e => e.Type == AchievementEventType.TerritoryLost && e.UserId == 2)
            .ToList();
        Assert.Single(lostForUser2);
        Assert.Equal(AchievementDeduplicationKeys.TerritoryLost(2, 90, 90), lostForUser2[0].DeduplicationKey);
    }

    [Fact]
    public async Task Defend_EmitsTerritoryDefended_AchievementEvent()
    {
        var db = CreateDb(nameof(Defend_EmitsTerritoryDefended_AchievementEvent));
        db.Users.Add(new User { Id = 1, Email = "u@x.com", PasswordHash = Array.Empty<byte>(), PasswordSalt = Array.Empty<byte>() });
        db.RunningActivities.Add(new RunningActivity
        {
            Id = 100, UserId = 1, ExternalActivityId = "ext100", Name = "R", Type = "Run",
            StartTime = DateTime.UtcNow, RunDate = DateOnly.FromDateTime(DateTime.UtcNow),
            DistanceMeters = 5000, MovingTimeSeconds = 1200, ElapsedTimeSeconds = 1200,
            TotalElevationGain = 0, AverageSpeed = 4, Source = "STRAVA",
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        });
        db.Territories.Add(new Territory
        {
            Id = 100, Name = "T100", IsActive = true,
            CurrentOwnerUserId = 1, CurrentOwnerScoreSnapshot = 0.3m, Version = 0
        });
        await db.SaveChangesAsync();

        var spy = new SpyAchievementEventPublisher();
        var engine = new StubTerritoryScoreEngine(new[]
        {
            new TerritoryContribution { TerritoryId = 100, FinalScore = 0.6, CoverageRatio = 0.6, DistanceInTerritory = 3000 }
        });
        var service = CreateService(db, engine, spy);

        await service.DefendAsync(1, 100, new[] { 100 });

        var defendEvents = spy.Published
            .Where(e => e.Type == AchievementEventType.TerritoryDefended && e.UserId == 1)
            .ToList();
        Assert.Single(defendEvents);
        Assert.Equal(AchievementDeduplicationKeys.TerritoryDefended(1, 100, 100), defendEvents[0].DeduplicationKey);
    }
}
