using GradProject.Application.DTOs.Gamification;
using GradProject.Domain.Entities;
using GradProject.Domain.Enums;
using GradProject.Infrastructure.Persistence;
using GradProject.Infrastructure.Services.Gamification;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace GradProject.Tests;

/// <summary>
/// Tests for AchievementEventService — idempotency, deduplication, and persistence.
/// Uses EF Core InMemory. The unique-constraint exception path (concurrent dedup) is
/// not exercised here because InMemory does not enforce unique indexes; only the
/// fast-path (pre-check) dedup is tested. The Postgres unique constraint provides the
/// actual safety guarantee in production.
/// </summary>
public class AchievementEventServiceTests
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

    private static AchievementEventService CreateService(AppDbContext db) =>
        new(db, NullLogger<AchievementEventService>.Instance);

    private static User SeedUser(AppDbContext db, int id = 1)
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

    private static AchievementEventDto MakeBadgeEarnedDto(int userId, int badgeId) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = AchievementEventType.BadgeEarned,
            OccurredAt = DateTime.UtcNow,
            BadgeId = badgeId,
            DeduplicationKey = AchievementDeduplicationKeys.BadgeEarned(userId, badgeId)
        };

    private static AchievementEventDto MakeChallengeCompletedDto(int userId, int challengeId) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = AchievementEventType.ChallengeCompleted,
            OccurredAt = DateTime.UtcNow,
            ChallengeId = challengeId,
            DeduplicationKey = AchievementDeduplicationKeys.ChallengeCompleted(userId, challengeId)
        };

    // -------------------------------------------------------------------------
    // Persistence
    // -------------------------------------------------------------------------

    [Fact]
    public async Task PublishAsync_PersistsEventToDatabase()
    {
        var db = CreateDb(nameof(PublishAsync_PersistsEventToDatabase));
        SeedUser(db);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var dto = MakeBadgeEarnedDto(userId: 1, badgeId: 10);
        await service.PublishAsync(dto);

        var stored = await db.AchievementEvents.SingleOrDefaultAsync(e => e.DeduplicationKey == dto.DeduplicationKey);
        Assert.NotNull(stored);
        Assert.Equal(AchievementEventType.BadgeEarned, stored.Type);
        Assert.Equal(1, stored.UserId);
        Assert.Equal(10, stored.BadgeId);
    }

    [Fact]
    public async Task PublishAsync_SetsOccurredAtToUtcNow_WhenNotProvided()
    {
        var db = CreateDb(nameof(PublishAsync_SetsOccurredAtToUtcNow_WhenNotProvided));
        SeedUser(db);
        await db.SaveChangesAsync();

        var dto = MakeBadgeEarnedDto(1, 11);
        dto.OccurredAt = default; // not set

        var before = DateTime.UtcNow.AddSeconds(-1);
        await CreateService(db).PublishAsync(dto);
        var after = DateTime.UtcNow.AddSeconds(1);

        var stored = await db.AchievementEvents.SingleAsync(e => e.DeduplicationKey == dto.DeduplicationKey);
        Assert.InRange(stored.OccurredAt, before, after);
    }

    // -------------------------------------------------------------------------
    // Deduplication (fast-path pre-check)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task PublishAsync_SameDeduplicationKey_StoresOnlyOneRecord()
    {
        var db = CreateDb(nameof(PublishAsync_SameDeduplicationKey_StoresOnlyOneRecord));
        SeedUser(db);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var dto = MakeBadgeEarnedDto(userId: 1, badgeId: 20);

        await service.PublishAsync(dto);
        // Second call with same key — should be silently ignored.
        await service.PublishAsync(new AchievementEventDto
        {
            Id = Guid.NewGuid(),
            UserId = dto.UserId,
            Type = dto.Type,
            OccurredAt = DateTime.UtcNow,
            BadgeId = dto.BadgeId,
            DeduplicationKey = dto.DeduplicationKey
        });

        var count = await db.AchievementEvents
            .CountAsync(e => e.DeduplicationKey == dto.DeduplicationKey);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task PublishAsync_DifferentDeduplicationKeys_StoresBothRecords()
    {
        var db = CreateDb(nameof(PublishAsync_DifferentDeduplicationKeys_StoresBothRecords));
        SeedUser(db);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        await service.PublishAsync(MakeBadgeEarnedDto(userId: 1, badgeId: 30));
        await service.PublishAsync(MakeBadgeEarnedDto(userId: 1, badgeId: 31));

        Assert.Equal(2, await db.AchievementEvents.CountAsync(e => e.UserId == 1));
    }

    // -------------------------------------------------------------------------
    // Nullable fields
    // -------------------------------------------------------------------------

    [Fact]
    public async Task PublishAsync_ChallengeCompleted_PersistsChallengeId()
    {
        var db = CreateDb(nameof(PublishAsync_ChallengeCompleted_PersistsChallengeId));
        SeedUser(db);
        await db.SaveChangesAsync();

        var dto = MakeChallengeCompletedDto(userId: 1, challengeId: 42);
        await CreateService(db).PublishAsync(dto);

        var stored = await db.AchievementEvents.SingleAsync(e => e.DeduplicationKey == dto.DeduplicationKey);
        Assert.Equal(42, stored.ChallengeId);
        Assert.Null(stored.BadgeId);
        Assert.Null(stored.TerritoryId);
    }

    [Fact]
    public async Task PublishAsync_LeaderboardClimbed_PersistsRankFields()
    {
        var db = CreateDb(nameof(PublishAsync_LeaderboardClimbed_PersistsRankFields));
        SeedUser(db);
        await db.SaveChangesAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        var dto = new AchievementEventDto
        {
            Id = Guid.NewGuid(),
            UserId = 1,
            Type = AchievementEventType.LeaderboardClimbed,
            OccurredAt = DateTime.UtcNow,
            ChallengeId = 5,
            PreviousRank = 10,
            CurrentRank = 3,
            DeduplicationKey = AchievementDeduplicationKeys.LeaderboardClimbed(1, "5", today)
        };
        await CreateService(db).PublishAsync(dto);

        var stored = await db.AchievementEvents.SingleAsync(e => e.DeduplicationKey == dto.DeduplicationKey);
        Assert.Equal(10, stored.PreviousRank);
        Assert.Equal(3, stored.CurrentRank);
        Assert.Equal(5, stored.ChallengeId);
    }

    [Fact]
    public async Task PublishAsync_PersonalBest_PersistsRunId()
    {
        var db = CreateDb(nameof(PublishAsync_PersonalBest_PersistsRunId));
        SeedUser(db);
        await db.SaveChangesAsync();

        var dto = new AchievementEventDto
        {
            Id = Guid.NewGuid(),
            UserId = 1,
            Type = AchievementEventType.PersonalBest,
            OccurredAt = DateTime.UtcNow,
            RunId = 77,
            DeduplicationKey = AchievementDeduplicationKeys.PersonalBest(1, 77)
        };
        await CreateService(db).PublishAsync(dto);

        var stored = await db.AchievementEvents.SingleAsync(e => e.DeduplicationKey == dto.DeduplicationKey);
        Assert.Equal(77, stored.RunId);
        Assert.Null(stored.ChallengeId);
    }

    // -------------------------------------------------------------------------
    // Territory events
    // -------------------------------------------------------------------------

    [Fact]
    public async Task PublishAsync_TerritoryClaimed_StoresTerritoryAndRunId()
    {
        var db = CreateDb(nameof(PublishAsync_TerritoryClaimed_StoresTerritoryAndRunId));
        SeedUser(db);
        await db.SaveChangesAsync();

        var dto = new AchievementEventDto
        {
            Id = Guid.NewGuid(),
            UserId = 1,
            Type = AchievementEventType.TerritoryClaimed,
            OccurredAt = DateTime.UtcNow,
            TerritoryId = 5,
            RunId = 99,
            DeduplicationKey = AchievementDeduplicationKeys.TerritoryClaimed(1, 5, 99)
        };
        await CreateService(db).PublishAsync(dto);

        var stored = await db.AchievementEvents.SingleAsync(e => e.DeduplicationKey == dto.DeduplicationKey);
        Assert.Equal(5, stored.TerritoryId);
        Assert.Equal(99, stored.RunId);
    }
}
