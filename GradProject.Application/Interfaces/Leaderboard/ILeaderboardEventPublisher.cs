namespace GradProject.Application.Interfaces.Leaderboard;

/// <summary>
/// Publishes leaderboard rank-change events.
/// challengeId == null indicates the global leaderboard scope.
/// Wired to IAchievementEventPublisher via LeaderboardEventPublisher (replaces the NoOp).
/// </summary>
public interface ILeaderboardEventPublisher
{
    Task PublishRankChangedAsync(int? challengeId, int userId, int oldRank, int newRank, CancellationToken ct = default);
}
