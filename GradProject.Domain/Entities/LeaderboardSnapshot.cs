namespace GradProject.Domain.Entities;

/// <summary>
/// Cached leaderboard entry for a specific scope and date.
/// ChallengeId == null represents the global (platform-wide) leaderboard snapshot.
/// ChallengeId != null represents a challenge-scoped snapshot.
/// One row per user per scope per snapshot date.
/// </summary>
public class LeaderboardSnapshot
{
    public int Id { get; set; }
    /// <summary>null = global leaderboard; non-null = challenge-scoped leaderboard.</summary>
    public int? ChallengeId { get; set; }
    public DateOnly SnapshotDate { get; set; }
    public int UserId { get; set; }
    public int Rank { get; set; }
    public string Username { get; set; } = null!;
    public double? TerritoryScore { get; set; }
    public int TerritoryCount { get; set; }
    public long TotalDistance { get; set; }
    public double AveragePace { get; set; }
    public long? CompletionSpeed { get; set; }
}
