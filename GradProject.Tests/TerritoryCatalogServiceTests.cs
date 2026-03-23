using GradProject.Application.Options;
using GradProject.Domain.Entities;
using GradProject.Domain.Enums;
using GradProject.Infrastructure.Persistence;
using GradProject.Infrastructure.Services.Gamification;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace GradProject.Tests;

public class TerritoryCatalogServiceTests
{
    // ────────────────────────────────────────────────
    // Helpers
    // ────────────────────────────────────────────────

    private static TerritoryCatalogService CreateService(
        AppDbContext db,
        bool enableCache = false,
        int cacheSeconds = 60,
        IMemoryCache? cache = null)
    {
        cache ??= new MemoryCache(new MemoryCacheOptions());
        var options = Options.Create(new TerritoryCatalogCacheOptions
        {
            EnableListCache = enableCache,
            ListCacheSeconds = cacheSeconds
        });
        return new TerritoryCatalogService(db, cache, options);
    }

    private static Territory MakeTerritory(int id, string name, int displayOrder = 0) => new()
    {
        Id = id,
        Name = name,
        PublicId = Guid.NewGuid(),
        IsActive = true,
        OwnershipTargetPercent = 100,
        DisplayOrder = displayOrder
    };

    private static User MakeUser(int id, string email) => new()
    {
        Id = id,
        Email = email,
        PasswordHash = Array.Empty<byte>(),
        PasswordSalt = Array.Empty<byte>()
    };

    // ────────────────────────────────────────────────
    // GetCatalogAsync — basic mapping
    // ────────────────────────────────────────────────

    [Fact]
    public async Task GetCatalogAsync_EmptyDb_ReturnsEmptyList()
    {
        var db = TestDbFactory.Create(nameof(GetCatalogAsync_EmptyDb_ReturnsEmptyList));

        var result = await CreateService(db).GetCatalogAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetCatalogAsync_MapsAllExpectedFields()
    {
        var db = TestDbFactory.Create(nameof(GetCatalogAsync_MapsAllExpectedFields));
        var pubId = Guid.NewGuid();
        db.Territories.Add(new Territory
        {
            Id = 1, Name = "Zone A", Description = "First zone",
            PublicId = pubId, OwnershipTargetPercent = 75,
            IsActive = true, DisplayOrder = 1
        });
        await db.SaveChangesAsync();

        var result = await CreateService(db).GetCatalogAsync();

        var item = Assert.Single(result);
        Assert.Equal(pubId, item.Id);
        Assert.Equal("Zone A", item.Name);
        Assert.Equal("First zone", item.Description);
        Assert.Equal(75, item.OwnershipTargetPercent);
        Assert.True(item.IsActive);
        Assert.Null(item.CurrentOwnerUserId);
        Assert.Null(item.CurrentOwnerUsername);
        Assert.Null(item.CurrentOwnerScoreSnapshot);
        Assert.Empty(item.GeometryCells);
    }

    [Fact]
    public async Task GetCatalogAsync_WithOwnerAndProfile_ResolvesFullName()
    {
        var db = TestDbFactory.Create(nameof(GetCatalogAsync_WithOwnerAndProfile_ResolvesFullName));
        db.Users.Add(MakeUser(10, "ali@x.com"));
        db.Profiles.Add(new Profile { UserId = 10, FirstName = "Ali", LastName = "Yılmaz" });
        db.Territories.Add(new Territory
        {
            Id = 1, Name = "T1", PublicId = Guid.NewGuid(), IsActive = true,
            CurrentOwnerUserId = 10, CurrentOwnerScoreSnapshot = 0.75m
        });
        await db.SaveChangesAsync();

        var result = await CreateService(db).GetCatalogAsync();

        Assert.Equal(10, result[0].CurrentOwnerUserId);
        Assert.Equal("Ali Yılmaz", result[0].CurrentOwnerUsername);
        Assert.Equal(0.75m, result[0].CurrentOwnerScoreSnapshot);
    }

    [Fact]
    public async Task GetCatalogAsync_WithOwnerNoProfile_FallsBackToEmail()
    {
        var db = TestDbFactory.Create(nameof(GetCatalogAsync_WithOwnerNoProfile_FallsBackToEmail));
        db.Users.Add(MakeUser(5, "solo@x.com"));
        db.Territories.Add(new Territory
        {
            Id = 1, Name = "T1", PublicId = Guid.NewGuid(), IsActive = true, CurrentOwnerUserId = 5
        });
        await db.SaveChangesAsync();

        var result = await CreateService(db).GetCatalogAsync();

        Assert.Equal("solo@x.com", result[0].CurrentOwnerUsername);
    }

    [Fact]
    public async Task GetCatalogAsync_OrderedByDisplayOrderThenId()
    {
        var db = TestDbFactory.Create(nameof(GetCatalogAsync_OrderedByDisplayOrderThenId));
        db.Territories.AddRange(
            new Territory { Id = 3, Name = "C", PublicId = Guid.NewGuid(), IsActive = true, DisplayOrder = 2 },
            new Territory { Id = 1, Name = "A", PublicId = Guid.NewGuid(), IsActive = true, DisplayOrder = 1 },
            new Territory { Id = 2, Name = "B", PublicId = Guid.NewGuid(), IsActive = true, DisplayOrder = 1 }
        );
        await db.SaveChangesAsync();

        var result = await CreateService(db).GetCatalogAsync();

        Assert.Equal(new[] { "A", "B", "C" }, result.Select(r => r.Name).ToArray());
    }

    // ────────────────────────────────────────────────
    // GetCatalogAsync — geometry cells
    // ────────────────────────────────────────────────

    [Fact]
    public async Task GetCatalogAsync_WithTableCells_ReturnsTableCells_IgnoresLegacyJson()
    {
        var db = TestDbFactory.Create(nameof(GetCatalogAsync_WithTableCells_ReturnsTableCells_IgnoresLegacyJson));
        db.Territories.Add(new Territory
        {
            Id = 1, Name = "T1", PublicId = Guid.NewGuid(), IsActive = true,
            GeometryCells = "[\"legacy_cell\"]"
        });
        db.TerritoryCells.AddRange(
            new TerritoryCell { TerritoryId = 1, H3Index = "8a2a100d2dfffff" },
            new TerritoryCell { TerritoryId = 1, H3Index = "8a2a100d2cfffff" }
        );
        await db.SaveChangesAsync();

        var result = await CreateService(db).GetCatalogAsync();

        Assert.Equal(2, result[0].GeometryCells.Count);
        Assert.DoesNotContain("legacy_cell", result[0].GeometryCells);
    }

    [Fact]
    public async Task GetCatalogAsync_NoTableCells_FallsBackToLegacyJson()
    {
        var db = TestDbFactory.Create(nameof(GetCatalogAsync_NoTableCells_FallsBackToLegacyJson));
        db.Territories.Add(new Territory
        {
            Id = 1, Name = "T1", PublicId = Guid.NewGuid(), IsActive = true,
            GeometryCells = "[\"cell_a\",\"cell_b\"]"
        });
        await db.SaveChangesAsync();

        var result = await CreateService(db).GetCatalogAsync();

        Assert.Equal(2, result[0].GeometryCells.Count);
        Assert.Contains("cell_a", result[0].GeometryCells);
        Assert.Contains("cell_b", result[0].GeometryCells);
    }

    [Fact]
    public async Task GetCatalogAsync_NoTableCellsNoJson_ReturnsEmptyCells()
    {
        var db = TestDbFactory.Create(nameof(GetCatalogAsync_NoTableCellsNoJson_ReturnsEmptyCells));
        db.Territories.Add(MakeTerritory(1, "T1"));
        await db.SaveChangesAsync();

        var result = await CreateService(db).GetCatalogAsync();

        Assert.Empty(result[0].GeometryCells);
    }

    [Fact]
    public async Task GetCatalogAsync_CellsForMultipleTerritories_AssignedCorrectly()
    {
        var db = TestDbFactory.Create(nameof(GetCatalogAsync_CellsForMultipleTerritories_AssignedCorrectly));
        db.Territories.AddRange(MakeTerritory(1, "A"), MakeTerritory(2, "B"));
        db.TerritoryCells.Add(new TerritoryCell { TerritoryId = 1, H3Index = "cell_for_1" });
        db.TerritoryCells.Add(new TerritoryCell { TerritoryId = 2, H3Index = "cell_for_2a" });
        db.TerritoryCells.Add(new TerritoryCell { TerritoryId = 2, H3Index = "cell_for_2b" });
        await db.SaveChangesAsync();

        var result = await CreateService(db).GetCatalogAsync();

        var a = result.Single(r => r.Name == "A");
        var b = result.Single(r => r.Name == "B");
        Assert.Equal(new[] { "cell_for_1" }, a.GeometryCells);
        Assert.Equal(2, b.GeometryCells.Count);
    }

    // ────────────────────────────────────────────────
    // GetCatalogAsync — cache
    // ────────────────────────────────────────────────

    [Fact]
    public async Task GetCatalogAsync_CacheEnabled_SecondCallReturnsCachedResult()
    {
        var db = TestDbFactory.Create(nameof(GetCatalogAsync_CacheEnabled_SecondCallReturnsCachedResult));
        db.Territories.Add(MakeTerritory(1, "T1"));
        await db.SaveChangesAsync();

        var cache = new MemoryCache(new MemoryCacheOptions());
        var svc = CreateService(db, enableCache: true, cache: cache);
        var first = await svc.GetCatalogAsync();

        // Remove territory from DB; second call must still come from cache
        db.Territories.RemoveRange(db.Territories);
        await db.SaveChangesAsync();
        var second = await svc.GetCatalogAsync();

        Assert.Single(first);
        Assert.Single(second);
        Assert.Equal(first[0].Id, second[0].Id);
    }

    [Fact]
    public async Task GetCatalogAsync_CacheDisabled_AlwaysHitsDb()
    {
        var db = TestDbFactory.Create(nameof(GetCatalogAsync_CacheDisabled_AlwaysHitsDb));
        db.Territories.Add(MakeTerritory(1, "T1"));
        await db.SaveChangesAsync();

        var svc = CreateService(db, enableCache: false);
        var first = await svc.GetCatalogAsync();

        db.Territories.RemoveRange(db.Territories);
        await db.SaveChangesAsync();
        var second = await svc.GetCatalogAsync();

        Assert.Single(first);
        Assert.Empty(second);
    }

    [Fact]
    public async Task GetCatalogAsync_EmptyResultIsCached_SecondCallDoesNotHitDb()
    {
        // Regression: empty list must also be cached, not bypassed
        var db = TestDbFactory.Create(nameof(GetCatalogAsync_EmptyResultIsCached_SecondCallDoesNotHitDb));
        var cache = new MemoryCache(new MemoryCacheOptions());
        var svc = CreateService(db, enableCache: true, cache: cache);

        var first = await svc.GetCatalogAsync();  // empty → populates cache

        db.Territories.Add(MakeTerritory(1, "New"));
        await db.SaveChangesAsync();
        var second = await svc.GetCatalogAsync(); // must still be empty from cache

        Assert.Empty(first);
        Assert.Empty(second);
    }

    [Fact]
    public async Task GetCatalogAsync_AfterInvalidation_ReturnsRefreshedData()
    {
        var db = TestDbFactory.Create(nameof(GetCatalogAsync_AfterInvalidation_ReturnsRefreshedData));
        db.Territories.Add(MakeTerritory(1, "Original"));
        await db.SaveChangesAsync();

        var cache = new MemoryCache(new MemoryCacheOptions());
        var svc = CreateService(db, enableCache: true, cache: cache);

        await svc.GetCatalogAsync(); // populates cache

        // Simulate ownership change: update DB and invalidate cache
        var t = await db.Territories.FindAsync(1);
        t!.Name = "Updated";
        await db.SaveChangesAsync();
        svc.InvalidateCatalogCache();

        var result = await svc.GetCatalogAsync();

        Assert.Equal("Updated", result[0].Name);
    }

    // ────────────────────────────────────────────────
    // GetMyProgressAsync — basic
    // ────────────────────────────────────────────────

    [Fact]
    public async Task GetMyProgressAsync_NoProgressRows_AllLocked_ZeroPercent()
    {
        var db = TestDbFactory.Create(nameof(GetMyProgressAsync_NoProgressRows_AllLocked_ZeroPercent));
        db.Territories.AddRange(MakeTerritory(1, "A"), MakeTerritory(2, "B"));
        await db.SaveChangesAsync();

        var result = await CreateService(db).GetMyProgressAsync(99);

        Assert.Equal(2, result.Count);
        Assert.All(result, r =>
        {
            Assert.Equal(TerritoryStatus.Locked, r.Status);
            Assert.Equal(0, r.ProgressPercent);
            Assert.False(r.IsCurrentOwner);
            Assert.Null(r.OwnedAt);
        });
    }

    [Fact]
    public async Task GetMyProgressAsync_OwnedRow_ReturnsOwnedStatusAndOwnedAt()
    {
        var db = TestDbFactory.Create(nameof(GetMyProgressAsync_OwnedRow_ReturnsOwnedStatusAndOwnedAt));
        var pubId = Guid.NewGuid();
        var ownedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        db.Users.Add(MakeUser(1, "u@x.com"));
        db.Territories.Add(new Territory
        {
            Id = 1, Name = "Zone", PublicId = pubId,
            IsActive = true, OwnershipTargetPercent = 80,
            CurrentOwnerUserId = 1  // user 1 is the current owner
        });
        db.UserTerritories.Add(new UserTerritory
        {
            UserId = 1, TerritoryId = 1,
            ProgressPercent = 80, Status = TerritoryStatus.Owned, OwnedAt = ownedAt
        });
        await db.SaveChangesAsync();

        var result = await CreateService(db).GetMyProgressAsync(1);

        var row = Assert.Single(result);
        Assert.Equal(pubId, row.TerritoryId);
        Assert.Equal("Zone", row.Name);
        Assert.Equal(TerritoryStatus.Owned, row.Status);
        Assert.Equal(80, row.ProgressPercent);
        Assert.Equal(ownedAt, row.OwnedAt);
    }

    [Fact]
    public async Task GetMyProgressAsync_InProgress_ProgressNotZeroed()
    {
        var db = TestDbFactory.Create(nameof(GetMyProgressAsync_InProgress_ProgressNotZeroed));
        db.Users.Add(MakeUser(1, "u@x.com"));
        db.Territories.Add(MakeTerritory(1, "T1"));
        db.UserTerritories.Add(new UserTerritory
        {
            UserId = 1, TerritoryId = 1, ProgressPercent = 45, Status = TerritoryStatus.InProgress
        });
        await db.SaveChangesAsync();

        var result = await CreateService(db).GetMyProgressAsync(1);

        Assert.Equal(TerritoryStatus.InProgress, result[0].Status);
        Assert.Equal(45, result[0].ProgressPercent);
    }

    [Fact]
    public async Task GetMyProgressAsync_LockedStatus_ProgressForcedToZero()
    {
        // If the stored row has Locked status, ProgressPercent must be zeroed in the API response.
        var db = TestDbFactory.Create(nameof(GetMyProgressAsync_LockedStatus_ProgressForcedToZero));
        db.Users.Add(MakeUser(1, "u@x.com"));
        db.Territories.Add(MakeTerritory(1, "T1"));
        db.UserTerritories.Add(new UserTerritory
        {
            UserId = 1, TerritoryId = 1, ProgressPercent = 0, Status = TerritoryStatus.Locked
        });
        await db.SaveChangesAsync();

        var result = await CreateService(db).GetMyProgressAsync(1);

        Assert.Equal(TerritoryStatus.Locked, result[0].Status);
        Assert.Equal(0, result[0].ProgressPercent);
    }

    // ────────────────────────────────────────────────
    // GetMyProgressAsync — IsCurrentOwner
    // ────────────────────────────────────────────────

    [Fact]
    public async Task GetMyProgressAsync_IsCurrentOwner_TrueOnlyForOwnedTerritory()
    {
        var db = TestDbFactory.Create(nameof(GetMyProgressAsync_IsCurrentOwner_TrueOnlyForOwnedTerritory));
        db.Users.Add(MakeUser(1, "u@x.com"));
        db.Territories.AddRange(
            new Territory { Id = 1, Name = "Mine", PublicId = Guid.NewGuid(), IsActive = true, CurrentOwnerUserId = 1 },
            new Territory { Id = 2, Name = "Theirs", PublicId = Guid.NewGuid(), IsActive = true, CurrentOwnerUserId = 99 },
            new Territory { Id = 3, Name = "Empty", PublicId = Guid.NewGuid(), IsActive = true, CurrentOwnerUserId = null }
        );
        await db.SaveChangesAsync();

        var result = await CreateService(db).GetMyProgressAsync(1);

        Assert.True(result.Single(r => r.Name == "Mine").IsCurrentOwner);
        Assert.False(result.Single(r => r.Name == "Theirs").IsCurrentOwner);
        Assert.False(result.Single(r => r.Name == "Empty").IsCurrentOwner);
    }

    // ────────────────────────────────────────────────
    // GetMyProgressAsync — isolation between users
    // ────────────────────────────────────────────────

    [Fact]
    public async Task GetMyProgressAsync_IsolatedPerUser_NoProgressLeaks()
    {
        var db = TestDbFactory.Create(nameof(GetMyProgressAsync_IsolatedPerUser_NoProgressLeaks));
        db.Users.Add(MakeUser(1, "u1@x.com"));
        db.Users.Add(MakeUser(2, "u2@x.com"));
        db.Territories.Add(MakeTerritory(1, "T1"));
        db.UserTerritories.Add(new UserTerritory
        {
            UserId = 1, TerritoryId = 1, ProgressPercent = 60, Status = TerritoryStatus.InProgress
        });
        db.UserTerritories.Add(new UserTerritory
        {
            UserId = 2, TerritoryId = 1, ProgressPercent = 30, Status = TerritoryStatus.InProgress
        });
        await db.SaveChangesAsync();

        var r1 = await CreateService(db).GetMyProgressAsync(1);
        var r2 = await CreateService(db).GetMyProgressAsync(2);

        Assert.Equal(60, r1[0].ProgressPercent);
        Assert.Equal(30, r2[0].ProgressPercent);
    }

    [Fact]
    public async Task GetMyProgressAsync_UserWithNoRows_SeesAllTerritoriesAsLocked()
    {
        var db = TestDbFactory.Create(nameof(GetMyProgressAsync_UserWithNoRows_SeesAllTerritoriesAsLocked));
        db.Users.Add(MakeUser(1, "u1@x.com"));
        db.Users.Add(MakeUser(2, "u2@x.com"));
        db.Territories.AddRange(MakeTerritory(1, "A"), MakeTerritory(2, "B"));
        // Only user 1 has progress
        db.UserTerritories.Add(new UserTerritory
        {
            UserId = 1, TerritoryId = 1, ProgressPercent = 50, Status = TerritoryStatus.InProgress
        });
        await db.SaveChangesAsync();

        var result = await CreateService(db).GetMyProgressAsync(2);

        Assert.All(result, r => Assert.Equal(TerritoryStatus.Locked, r.Status));
    }
}
