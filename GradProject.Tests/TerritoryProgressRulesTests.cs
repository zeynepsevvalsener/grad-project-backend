using GradProject.Application.Services.Gamification;
using GradProject.Application.Utilities;
using GradProject.Domain.Entities;
using GradProject.Domain.Enums;

namespace GradProject.Tests;

public class TerritoryProgressRulesTests
{
    [Fact]
    public void ResolveDisplayStatus_NoRow_ReturnsLocked()
    {
        Assert.Equal(TerritoryStatus.Locked, TerritoryProgressRules.ResolveDisplayStatus(null, 100));
    }

    [Fact]
    public void ResolveDisplayStatus_ProgressAtTarget_ReturnsOwned()
    {
        var row = new UserTerritory { ProgressPercent = 80, Status = TerritoryStatus.InProgress };
        Assert.Equal(TerritoryStatus.Owned, TerritoryProgressRules.ResolveDisplayStatus(row, 80, isCurrentOwner: true));
    }

    [Fact]
    public void ResolveDisplayStatus_ProgressAboveTarget_ReturnsOwned()
    {
        var row = new UserTerritory { ProgressPercent = 95, Status = TerritoryStatus.InProgress };
        Assert.Equal(TerritoryStatus.Owned, TerritoryProgressRules.ResolveDisplayStatus(row, 80, isCurrentOwner: true));
    }

    [Fact]
    public void ResolveDisplayStatus_ProgressAtTarget_ButNotCurrentOwner_ReturnsInProgress()
    {
        // User hit the ownership threshold but was displaced by someone with a higher score.
        var row = new UserTerritory { ProgressPercent = 80, Status = TerritoryStatus.InProgress };
        Assert.Equal(TerritoryStatus.InProgress, TerritoryProgressRules.ResolveDisplayStatus(row, 80, isCurrentOwner: false));
    }

    [Fact]
    public void ResolveDisplayStatus_ProgressBetween0And100_ReturnsInProgress()
    {
        var row = new UserTerritory { ProgressPercent = 50, Status = TerritoryStatus.Unlocked };
        Assert.Equal(TerritoryStatus.InProgress, TerritoryProgressRules.ResolveDisplayStatus(row, 100));
    }

    [Fact]
    public void ResolveDisplayStatus_Progress100BelowTarget_ReturnsInProgress()
    {
        // progress == 100 but target is 150 — misconfigured target, treated as InProgress
        var row = new UserTerritory { ProgressPercent = 110, Status = TerritoryStatus.InProgress };
        Assert.Equal(TerritoryStatus.InProgress, TerritoryProgressRules.ResolveDisplayStatus(row, 150));
    }

    [Fact]
    public void ResolveDisplayStatus_ZeroProgressUnlocked_ReturnsUnlocked()
    {
        var row = new UserTerritory { ProgressPercent = 0, Status = TerritoryStatus.Unlocked };
        Assert.Equal(TerritoryStatus.Unlocked, TerritoryProgressRules.ResolveDisplayStatus(row, 100));
    }

    [Fact]
    public void ResolveDisplayStatus_ZeroProgressLocked_ReturnsLocked()
    {
        var row = new UserTerritory { ProgressPercent = 0, Status = TerritoryStatus.Locked };
        Assert.Equal(TerritoryStatus.Locked, TerritoryProgressRules.ResolveDisplayStatus(row, 100));
    }
}

public class UserDisplayNameHelperTests
{
    [Fact]
    public void Resolve_WithFirstAndLastName_ReturnsFullName()
    {
        Assert.Equal("Jane Doe", UserDisplayNameHelper.Resolve("Jane", "Doe", "jane@test.com"));
    }

    [Fact]
    public void Resolve_WithFirstNameOnly_ReturnsFirstName()
    {
        Assert.Equal("Jane", UserDisplayNameHelper.Resolve("Jane", null, "jane@test.com"));
    }

    [Fact]
    public void Resolve_WithoutFirstName_ReturnsFallback()
    {
        Assert.Equal("jane@test.com", UserDisplayNameHelper.Resolve(null, "Doe", "jane@test.com"));
    }

    [Fact]
    public void Resolve_EmptyFirstName_ReturnsFallback()
    {
        Assert.Equal("jane@test.com", UserDisplayNameHelper.Resolve("", "Doe", "jane@test.com"));
    }

    [Fact]
    public void Resolve_WhitespaceOnlyFirstName_ReturnsFallback()
    {
        Assert.Equal("jane@test.com", UserDisplayNameHelper.Resolve("   ", "Doe", "jane@test.com"));
    }

    [Fact]
    public void ResolveNullable_WithFirstName_ReturnsFullName()
    {
        Assert.Equal("Jane Doe", UserDisplayNameHelper.ResolveNullable("Jane", "Doe", null));
    }

    [Fact]
    public void ResolveNullable_NoFirstNameNoFallback_ReturnsNull()
    {
        Assert.Null(UserDisplayNameHelper.ResolveNullable(null, "Doe", null));
    }
}
