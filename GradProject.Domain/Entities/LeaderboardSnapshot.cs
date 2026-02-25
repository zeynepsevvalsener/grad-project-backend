namespace GradProject.Domain.Entities;

/// <summary>
/// Cached leaderboard entry for a challenge. One row per user per challenge per snapshot date.
/// </summary>
public class LeaderboardSnapshot
{
    public int Id { get; set; }
    public int ChallengeId { get; set; }
    public DateOnly SnapshotDate { get; set; }
    public int UserId { get; set; }
    public int Rank { get; set; }
    public string Username { get; set; } = null!;
    public double? TerritoryScore { get; set; }
    public long TotalDistance { get; set; }
    public double AveragePace { get; set; }
    public long? CompletionSpeed { get; set; }
}
