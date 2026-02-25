using GradProject.Application.DTOs.Leaderboard;
using GradProject.Application.Services.Leaderboard;
using GradProject.Domain.Enums;

namespace GradProject.Tests;

public class LeaderboardServiceTests
{
    [Fact]
    public void RankingEngine_OrdersByDistanceMetric()
    {
        var data = new List<LeaderboardAggregateData>
        {
            new() { UserId = 1, Email = "a@x.com", TotalDistance = 3000, TotalMovingTime = 900, JoinedAt = DateTime.UtcNow, ChallengeStartDate = DateTime.UtcNow.AddDays(-7) },
            new() { UserId = 2, Email = "b@x.com", TotalDistance = 5000, TotalMovingTime = 1200, JoinedAt = DateTime.UtcNow, ChallengeStartDate = DateTime.UtcNow.AddDays(-7) }
        };
        var engine = new RankingEngine();
        var ranked = engine.CalculateRanks(data, ChallengeMetric.Distance);

        Assert.Equal(2, ranked.Count);
        Assert.Equal(1, ranked[0].Rank);
        Assert.Equal(2, ranked[0].UserId);
        Assert.Equal(5000, ranked[0].TotalDistance);
    }

    [Fact]
    public void RankingEngine_OrdersByPaceMetric()
    {
        var data = new List<LeaderboardAggregateData>
        {
            new() { UserId = 1, Email = "a@x.com", TotalDistance = 5000, TotalMovingTime = 1500, JoinedAt = DateTime.UtcNow, ChallengeStartDate = DateTime.UtcNow.AddDays(-7) },
            new() { UserId = 2, Email = "b@x.com", TotalDistance = 5000, TotalMovingTime = 1200, JoinedAt = DateTime.UtcNow, ChallengeStartDate = DateTime.UtcNow.AddDays(-7) }
        };
        var engine = new RankingEngine();
        var ranked = engine.CalculateRanks(data, ChallengeMetric.Pace);

        Assert.Equal(2, ranked.Count);
        Assert.Equal(1, ranked[0].Rank);
        Assert.Equal(2, ranked[0].UserId);
    }
}
