using GradProject.Application.Interfaces.Leaderboard;

namespace GradProject.Infrastructure.Services.Leaderboard;

/// <summary>
/// No-op implementation. Replace with real publisher (e.g. message queue, push service).
/// </summary>
public class NoOpLeaderboardEventPublisher : ILeaderboardEventPublisher
{
    public Task PublishRankChangedAsync(int? challengeId, int userId, int oldRank, int newRank, CancellationToken ct = default)
        => Task.CompletedTask;
}
