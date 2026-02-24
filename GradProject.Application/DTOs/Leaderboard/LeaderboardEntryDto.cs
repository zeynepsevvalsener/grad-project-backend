using System.Text.Json.Serialization;

namespace GradProject.Application.DTOs.Leaderboard;

/// <summary>
/// Represents a single entry in a challenge leaderboard with user ranking and performance metrics.
/// </summary>
public class LeaderboardEntryDto
{
    /// <summary>
    /// User's rank in the challenge (1-based index).
    /// </summary>
    [JsonPropertyName("rank")]
    public int Rank { get; set; }

    /// <summary>
    /// User identifier.
    /// </summary>
    [JsonPropertyName("userId")]
    public int UserId { get; set; }

    /// <summary>
    /// User's display name (FirstName LastName or Email).
    /// </summary>
    [JsonPropertyName("username")]
    public string Username { get; set; } = null!;

    /// <summary>
    /// Territory score (2 decimal places). Null if not applicable.
    /// </summary>
    [JsonPropertyName("territoryScore")]
    public double? TerritoryScore { get; set; }

    /// <summary>
    /// Total distance in meters.
    /// </summary>
    [JsonPropertyName("totalDistance")]
    public long TotalDistance { get; set; }

    /// <summary>
    /// Average pace in seconds per kilometer (2 decimal places).
    /// </summary>
    [JsonPropertyName("averagePace")]
    public double AveragePace { get; set; }

    /// <summary>
    /// Completion speed in seconds (null if not completed).
    /// </summary>
    [JsonPropertyName("completionSpeed")]
    public long? CompletionSpeed { get; set; }
}
