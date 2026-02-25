namespace GradProject.Application.Interfaces.Leaderboard;

/// <summary>
/// Publishes leaderboard events (e.g. rank change, new #1). Implement for push/notification.
/// </summary>
public interface ILeaderboardEventPublisher
{
    Task PublishRankChangedAsync(int challengeId, int userId, int oldRank, int newRank, CancellationToken ct = default);
}
