using GradProject.Application.DTOs.Gamification;
using GradProject.Domain.Entities;
using GradProject.Domain.Enums;
using GradProject.Infrastructure.Persistence;
using GradProject.Infrastructure.Services.Gamification;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace GradProject.Tests;

public class BadgeServiceTests
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

    private static BadgeService CreateService(AppDbContext db)
    {
        return new BadgeService(db, NullLogger<BadgeService>.Instance);
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

    private static Badge SeedBadge(AppDbContext db, int id, bool isActive = true)
    {
        var badge = new Badge
        {
            Id = id,
            Name = "First Run",
            Description = "First run completed",
            Type = BadgeType.FirstRun,
            IconUrl = "https://example.com/icon.png",
            PointsReward = 10,
            IsActive = isActive
        };
        db.Badges.Add(badge);
        return badge;
    }

    private static void SeedUserBadge(AppDbContext db, int userId, int badgeId, DateTime earnedAtUtc)
    {
        db.UserBadges.Add(new UserBadge
        {
            UserId = userId,
            BadgeId = badgeId,
            EarnedAtUtc = earnedAtUtc
        });
    }

    // --- GetUserBadgesAsync ---

    [Fact]
    public async Task GetUserBadgesAsync_WhenUserNotFound_ReturnsEmptyList()
    {
        var db = CreateDb(nameof(GetUserBadgesAsync_WhenUserNotFound_ReturnsEmptyList));
        SeedUser(db, 1);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.GetUserBadgesAsync(999);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetUserBadgesAsync_WhenUserHasNoBadges_ReturnsEmptyList()
    {
        var db = CreateDb(nameof(GetUserBadgesAsync_WhenUserHasNoBadges_ReturnsEmptyList));
        SeedUser(db, 1);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.GetUserBadgesAsync(1);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetUserBadgesAsync_WhenUserHasBadges_ReturnsOrderedByEarnedAtDesc()
    {
        var db = CreateDb(nameof(GetUserBadgesAsync_WhenUserHasBadges_ReturnsOrderedByEarnedAtDesc));
        SeedUser(db, 1);
        SeedBadge(db, 1);
        SeedBadge(db, 2);
        var older = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var newer = new DateTime(2024, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        SeedUserBadge(db, 1, 1, older);
        SeedUserBadge(db, 1, 2, newer);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.GetUserBadgesAsync(1);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.True(result[0].EarnedAtUtc >= result[1].EarnedAtUtc);
        Assert.Equal(newer, result[0].EarnedAtUtc);
        Assert.Equal(older, result[1].EarnedAtUtc);
    }

    [Fact]
    public async Task GetUserBadgesAsync_WhenUserHasBadges_ReturnsCorrectDtoShape()
    {
        var db = CreateDb(nameof(GetUserBadgesAsync_WhenUserHasBadges_ReturnsCorrectDtoShape));
        SeedUser(db, 1);
        var badge = SeedBadge(db, 1);
        badge.Description = "Test description";
        badge.IconUrl = "https://example.com/icon.png";
        var earnedAt = new DateTime(2024, 3, 15, 10, 0, 0, DateTimeKind.Utc);
        SeedUserBadge(db, 1, 1, earnedAt);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.GetUserBadgesAsync(1);

        Assert.NotNull(result);
        var dto = Assert.Single(result);
        Assert.Equal(1, dto.BadgeId);
        Assert.Equal("First Run", dto.Name);
        Assert.Equal("Test description", dto.Description);
        Assert.Equal(BadgeType.FirstRun, dto.Type);
        Assert.Equal("https://example.com/icon.png", dto.IconUrl);
        Assert.Equal(10, dto.PointsReward);
        Assert.Equal(earnedAt, dto.EarnedAtUtc);
    }

    // --- AwardBadgeAsync ---

    [Fact]
    public async Task AwardBadgeAsync_WhenUserNotFound_ReturnsUserNotFound()
    {
        var db = CreateDb(nameof(AwardBadgeAsync_WhenUserNotFound_ReturnsUserNotFound));
        SeedBadge(db, 1);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.AwardBadgeAsync(999, 1);

        Assert.Equal(AwardBadgeResult.UserNotFound, result);
        Assert.Empty(await db.UserBadges.ToListAsync());
    }

    [Fact]
    public async Task AwardBadgeAsync_WhenBadgeNotFound_ReturnsBadgeNotFound()
    {
        var db = CreateDb(nameof(AwardBadgeAsync_WhenBadgeNotFound_ReturnsBadgeNotFound));
        SeedUser(db, 1);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.AwardBadgeAsync(1, 999);

        Assert.Equal(AwardBadgeResult.BadgeNotFound, result);
        Assert.Empty(await db.UserBadges.ToListAsync());
    }

    [Fact]
    public async Task AwardBadgeAsync_WhenBadgeInactive_ReturnsBadgeNotFound()
    {
        var db = CreateDb(nameof(AwardBadgeAsync_WhenBadgeInactive_ReturnsBadgeNotFound));
        SeedUser(db, 1);
        SeedBadge(db, 1, isActive: false);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.AwardBadgeAsync(1, 1);

        Assert.Equal(AwardBadgeResult.BadgeNotFound, result);
        Assert.Empty(await db.UserBadges.ToListAsync());
    }

    [Fact]
    public async Task AwardBadgeAsync_WhenAlreadyEarned_ReturnsAlreadyExists()
    {
        var db = CreateDb(nameof(AwardBadgeAsync_WhenAlreadyEarned_ReturnsAlreadyExists));
        SeedUser(db, 1);
        SeedBadge(db, 1);
        SeedUserBadge(db, 1, 1, DateTime.UtcNow.AddDays(-1));
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.AwardBadgeAsync(1, 1);

        Assert.Equal(AwardBadgeResult.AlreadyExists, result);
        var count = await db.UserBadges.CountAsync(ub => ub.UserId == 1 && ub.BadgeId == 1);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task AwardBadgeAsync_WhenValid_ReturnsCreatedAndStoresRecord()
    {
        var db = CreateDb(nameof(AwardBadgeAsync_WhenValid_ReturnsCreatedAndStoresRecord));
        SeedUser(db, 1);
        SeedBadge(db, 1);
        await db.SaveChangesAsync();

        var before = DateTime.UtcNow;
        var service = CreateService(db);
        var result = await service.AwardBadgeAsync(1, 1);
        var after = DateTime.UtcNow;

        Assert.Equal(AwardBadgeResult.Created, result);
        var userBadge = await db.UserBadges.SingleOrDefaultAsync(ub => ub.UserId == 1 && ub.BadgeId == 1);
        Assert.NotNull(userBadge);
        Assert.Equal(1, userBadge.UserId);
        Assert.Equal(1, userBadge.BadgeId);
        Assert.True(userBadge.EarnedAtUtc >= before.AddSeconds(-1) && userBadge.EarnedAtUtc <= after.AddSeconds(1));
    }

    [Fact]
    public async Task AwardBadgeAsync_WhenCalledTwice_SecondCallReturnsAlreadyExists()
    {
        var db = CreateDb(nameof(AwardBadgeAsync_WhenCalledTwice_SecondCallReturnsAlreadyExists));
        SeedUser(db, 1);
        SeedBadge(db, 1);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var first = await service.AwardBadgeAsync(1, 1);
        var second = await service.AwardBadgeAsync(1, 1);

        Assert.Equal(AwardBadgeResult.Created, first);
        Assert.Equal(AwardBadgeResult.AlreadyExists, second);
        var count = await db.UserBadges.CountAsync(ub => ub.UserId == 1 && ub.BadgeId == 1);
        Assert.Equal(1, count);
    }
}
