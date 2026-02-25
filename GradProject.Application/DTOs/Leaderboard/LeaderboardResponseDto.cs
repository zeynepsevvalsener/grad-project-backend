using System.Text.Json.Serialization;

namespace GradProject.Application.DTOs.Leaderboard;

/// <summary>
/// Represents the complete leaderboard response for a challenge, including paginated entries,
/// optional current user entry, and pagination metadata.
/// </summary>
public class LeaderboardResponseDto
{
    /// <summary>
    /// List of leaderboard entries for the requested page.
    /// </summary>
    [JsonPropertyName("entries")]
    public List<LeaderboardEntryDto> Entries { get; set; } = new();

    /// <summary>
    /// Current user's entry (if userId parameter provided). Null if userId not provided.
    /// </summary>
    [JsonPropertyName("currentUserEntry")]
    public LeaderboardEntryDto? CurrentUserEntry { get; set; }

    /// <summary>
    /// Pagination metadata including total count, current page, page size, and navigation flags.
    /// </summary>
    [JsonPropertyName("pagination")]
    public PaginationMetadata Pagination { get; set; } = null!;
}
