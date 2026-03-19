using GradProject.Application.DTOs.Gamification;
using GradProject.Application.Interfaces;
using GradProject.Application.Interfaces.Leaderboard;
using GradProject.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace GradProject.Infrastructure.Services.Leaderboard;

/// <summary>
/// Real implementation of ILeaderboardEventPublisher that translates rank-change signals
/// into persisted AchievementEvents via IAchievementEventPublisher.
///
/// Replaces NoOpLeaderboardEventPublisher in DI.
/// The NoOp remains available for tests that don't need event verification.
///
/// Climb detection (called by LeaderboardService.PersistSnapshotAsync):
///   - Only emits when newRank &lt; oldRank (rank number decreased = moved up the board).
///   - Dedup key includes the snapshot date, so one LeaderboardClimbed event per user per
///     scope per day is the maximum, regardless of how many times the refresh job runs.
/// </summary>
public class LeaderboardEventPublisher : ILeaderboardEventPublisher
{
    private readonly IAchievementEventPublisher _publisher;
    private readonly ILogger<LeaderboardEventPublisher> _logger;

    public LeaderboardEventPublisher(
        IAchievementEventPublisher publisher,
        ILogger<LeaderboardEventPublisher> logger)
    {
        _publisher = publisher;
        _logger = logger;
    }

    public async Task PublishRankChangedAsync(
        int? challengeId,
        int userId,
        int oldRank,
        int newRank,
        CancellationToken ct = default)
    {
        if (newRank >= oldRank)
            return; // Not a climb — no event.

        var scopeKey = challengeId?.ToString() ?? AchievementDeduplicationKeys.GlobalLeaderboardScope;
        var today = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");
        var dedupKey = AchievementDeduplicationKeys.LeaderboardClimbed(userId, scopeKey, today);

        _logger.LogInformation(
            "LeaderboardClimbed: UserId={UserId} Scope={Scope} {OldRank}->{NewRank}",
            userId, scopeKey, oldRank, newRank);

        await _publisher.PublishAsync(new AchievementEventDto
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = AchievementEventType.LeaderboardClimbed,
            OccurredAt = DateTime.UtcNow,
            ChallengeId = challengeId,
            PreviousRank = oldRank,
            CurrentRank = newRank,
            DeduplicationKey = dedupKey
        }, ct);
    }
}
