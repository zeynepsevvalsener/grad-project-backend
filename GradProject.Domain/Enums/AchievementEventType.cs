namespace GradProject.Domain.Enums;

/// <summary>
/// Canonical set of achievement event types emitted by the domain layer.
/// Used as the event type code in AchievementEvent and the notification pipeline.
///
/// Design notes:
/// - Values are stable integers stored in the DB — do not reorder or reuse numbers.
/// - TerritoryRankReached is reserved for BE-7 (Territory Rank System); no trigger is wired yet.
/// - TerritoryClaimed covers both unclaimed territory grabs and transfers (taking from another user).
/// </summary>
public enum AchievementEventType
{
    ChallengeCompleted   = 1,
    BadgeEarned          = 2,
    LeaderboardClimbed   = 3,
    PersonalBest         = 4,
    TerritoryClaimed     = 5,
    TerritoryDefended    = 6,
    TerritoryLost        = 7,
    TerritoryRankReached = 8   // Extension point — wired in BE-7
}
