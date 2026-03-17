using GradProject.Application.DTOs.Leaderboard;
using GradProject.Application.Services.Leaderboard;

namespace GradProject.Tests;

public class LeaderboardServiceTests
{
    [Fact]
    public void RankingEngine_OrdersBy_TotalDistance_When_TerritoryEqual()
    {
        var data = new List<LeaderboardAggregateData>
        {
            new() { UserId = 1, Email = "a@x.com", TerritoryScore = 10, TotalDistance = 3000, TotalMovingTime = 900, JoinedAt = DateTime.UtcNow, ChallengeStartDate = DateTime.UtcNow.AddDays(-7) },
            new() { UserId = 2, Email = "b@x.com", TerritoryScore = 10, TotalDistance = 5000, TotalMovingTime = 1200, JoinedAt = DateTime.UtcNow, ChallengeStartDate = DateTime.UtcNow.AddDays(-7) }
        };
        var engine = new RankingEngine();
        var ranked = engine.CalculateRanks(data);

        Assert.Equal(2, ranked.Count);
        Assert.Equal(1, ranked[0].Rank);
        Assert.Equal(2, ranked[0].UserId);
        Assert.Equal(5000, ranked[0].TotalDistance);
    }

    [Fact]
    public void RankingEngine_OrdersBy_Pace_When_Territory_And_Distance_Equal()
    {
        var data = new List<LeaderboardAggregateData>
        {
            // User 1 slower pace (higher seconds/km)
            new() { UserId = 1, Email = "a@x.com", TerritoryScore = 10, TotalDistance = 5000, TotalMovingTime = 1500, JoinedAt = DateTime.UtcNow, ChallengeStartDate = DateTime.UtcNow.AddDays(-7) },
            // User 2 faster pace (lower seconds/km)
            new() { UserId = 2, Email = "b@x.com", TerritoryScore = 10, TotalDistance = 5000, TotalMovingTime = 1200, JoinedAt = DateTime.UtcNow, ChallengeStartDate = DateTime.UtcNow.AddDays(-7) }
        };
        var engine = new RankingEngine();
        var ranked = engine.CalculateRanks(data);

        Assert.Equal(2, ranked.Count);
        Assert.Equal(1, ranked[0].Rank);
        Assert.Equal(2, ranked[0].UserId);
    }

    [Fact]
    public void RankingEngine_OrdersBy_CompletionSpeed_When_OtherMetricsEqual()
    {
        var start = DateTime.UtcNow.AddDays(-7);
        var data = new List<LeaderboardAggregateData>
        {
            // User 1 finishes later (worse completion speed)
            new() { UserId = 1, Email = "a@x.com", TerritoryScore = 10, TotalDistance = 5000, TotalMovingTime = 1200, ChallengeStartDate = start, CompletedAt = start.AddHours(5), JoinedAt = start },
            // User 2 finishes earlier (better completion speed)
            new() { UserId = 2, Email = "b@x.com", TerritoryScore = 10, TotalDistance = 5000, TotalMovingTime = 1200, ChallengeStartDate = start, CompletedAt = start.AddHours(3), JoinedAt = start }
        };
        var engine = new RankingEngine();
        var ranked = engine.CalculateRanks(data);

        Assert.Equal(2, ranked.Count);
        Assert.Equal(1, ranked[0].Rank);
        Assert.Equal(2, ranked[0].UserId);
    }

    [Fact]
    public void RankingEngine_Uses_UserId_As_Final_TieBreaker()
    {
        var start = DateTime.UtcNow.AddDays(-7);
        var complete = start.AddHours(3);

        var data = new List<LeaderboardAggregateData>
        {
            new() { UserId = 10, Email = "a@x.com", TerritoryScore = 10, TotalDistance = 5000, TotalMovingTime = 1200, ChallengeStartDate = start, CompletedAt = complete, JoinedAt = start },
            new() { UserId = 5, Email = "b@x.com", TerritoryScore = 10, TotalDistance = 5000, TotalMovingTime = 1200, ChallengeStartDate = start, CompletedAt = complete, JoinedAt = start }
        };
        var engine = new RankingEngine();
        var ranked = engine.CalculateRanks(data);

        Assert.Equal(2, ranked.Count);
        // User with smaller UserId should win when all metrics are equal
        Assert.Equal(5, ranked[0].UserId);
        Assert.Equal(1, ranked[0].Rank);
        Assert.Equal(10, ranked[1].UserId);
    }

    [Fact]
    public void RankingEngine_Pushes_Users_With_NoRuns_To_Bottom()
    {
        var start = DateTime.UtcNow.AddDays(-7);
        var data = new List<LeaderboardAggregateData>
        {
            // Active runner
            new() { UserId = 1, Email = "a@x.com", TerritoryScore = 0, TotalDistance = 5000, TotalMovingTime = 1200, ChallengeStartDate = start, JoinedAt = start },
            // No runs: distance=0, movingTime=0 => worst pace
            new() { UserId = 2, Email = "b@x.com", TerritoryScore = 0, TotalDistance = 0, TotalMovingTime = 0, ChallengeStartDate = start, JoinedAt = start }
        };
        var engine = new RankingEngine();
        var ranked = engine.CalculateRanks(data);

        Assert.Equal(2, ranked.Count);
        Assert.Equal(1, ranked[0].UserId);
        Assert.Equal(2, ranked[1].UserId);
    }
}
