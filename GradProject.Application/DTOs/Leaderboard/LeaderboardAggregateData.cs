namespace GradProject.Application.DTOs.Leaderboard;

/// <summary>
/// Internal class for aggregating leaderboard data from database queries.
/// Contains database-mapped properties and computed properties for ranking calculations.
/// <para>Edge-case handling for ranking:
///   - TerritoryCount → current owned active territories (from Territories); higher ranks first.
///   - Null TerritoryScore → treated as 0 (ranks last among scored users).
///   - Zero TotalDistance → AveragePace = double.MaxValue (ranks last on pace).
///   - Null CompletedAt → CompletionSpeed = null → treated as long.MaxValue (ranks last on speed).
///   - Ties across all metrics → broken deterministically by UserId ASC.
///   - Users with no runs are included (0 distance, worst pace) and appear at the bottom.
/// </para>
/// </summary>
public class LeaderboardAggregateData
{
    /// <summary>User identifier</summary>
    public int UserId { get; set; }

    /// <summary>User's email address</summary>
    public string Email { get; set; } = null!;

    /// <summary>User's first name (nullable)</summary>
    public string? FirstName { get; set; }

    /// <summary>User's last name (nullable)</summary>
    public string? LastName { get; set; }

    /// <summary>Territory score from UserChallenge entity</summary>
    public double? TerritoryScore { get; set; }

    /// <summary>Count of active territories where this user is current owner (map ownership).</summary>
    public int TerritoryCount { get; set; }

    /// <summary>Total duration in seconds from UserChallenge entity</summary>
    public long? TotalDurationSeconds { get; set; }

    /// <summary>Challenge completion timestamp (nullable if not completed)</summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>Timestamp when user joined the challenge</summary>
    public DateTime JoinedAt { get; set; }

    /// <summary>Challenge start date</summary>
    public DateTime ChallengeStartDate { get; set; }

    /// <summary>Total distance in meters (aggregated from RunningActivities)</summary>
    public double TotalDistance { get; set; }

    /// <summary>Total moving time in seconds (aggregated from RunningActivities)</summary>
    public int TotalMovingTime { get; set; }

    /// <summary>
    /// Computed username: FirstName LastName if available, otherwise Email.
    /// Trims whitespace from concatenated name.
    /// </summary>
    public string Username => !string.IsNullOrEmpty(FirstName)
        ? $"{FirstName} {LastName}".Trim()  // Use full name if FirstName exists
        : Email;  // Fallback to email if no name available

    /// <summary>
    /// Computed average pace in seconds per kilometer.
    /// Returns Double.MaxValue if TotalDistance is zero (for ranking purposes).
    /// Formula: TotalMovingTime / (TotalDistance / 1000.0)
    /// </summary>
    public double AveragePace => TotalDistance > 0
        ? TotalMovingTime / (TotalDistance / 1000.0)  // Convert meters to kilometers, then calculate pace
        : double.MaxValue;  // Zero distance = worst possible pace (ranks last)

    /// <summary>
    /// Computed completion speed in seconds (time from challenge start to completion).
    /// Returns null if challenge is not completed.
    /// Formula: (CompletedAt - ChallengeStartDate).TotalSeconds
    /// </summary>
    public long? CompletionSpeed => CompletedAt.HasValue
        ? (long)(CompletedAt.Value - ChallengeStartDate).TotalSeconds  // Time elapsed from start to completion
        : null;  // Not completed = null (treated as infinity in ranking)
}
