namespace GradProject.Application.DTOs.Gamification;

/// <summary>
/// Single source of truth for all AchievementEvent deduplication key formats.
/// Every caller that builds an AchievementEventDto must use these helpers
/// so that key format changes propagate automatically.
/// </summary>
public static class AchievementDeduplicationKeys
{
    public static string BadgeEarned(int userId, int badgeId) =>
        $"badge_earned:{userId}:{badgeId}";

    public static string PersonalBest(int userId, int runId) =>
        $"pb:{userId}:{runId}";

    public static string ChallengeCompleted(int userId, int challengeId) =>
        $"challenge_completed:{userId}:{challengeId}";

    public static string TerritoryClaimed(int userId, int territoryId, int runId) =>
        $"territory_claimed:{userId}:{territoryId}:{runId}";

    public static string TerritoryDefended(int userId, int territoryId, int runId) =>
        $"territory_defended:{userId}:{territoryId}:{runId}";

    public static string TerritoryLost(int userId, int territoryId, int runId) =>
        $"territory_lost:{userId}:{territoryId}:{runId}";

    /// <param name="scopeKey">Use <see cref="GlobalLeaderboardScope"/> for the global leaderboard,
    /// or the challenge ID as a string for challenge-scoped boards.</param>
    public static string LeaderboardClimbed(int userId, string scopeKey, string snapshotDate) =>
        $"leaderboard_climbed:{userId}:{scopeKey}:{snapshotDate}";

    /// <summary>Scope key used for the global (non-challenge) leaderboard.</summary>
    public const string GlobalLeaderboardScope = "global";
}
