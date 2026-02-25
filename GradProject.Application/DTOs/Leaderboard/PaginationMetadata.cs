using System.Text.Json.Serialization;

namespace GradProject.Application.DTOs.Leaderboard;

/// <summary>
/// Provides pagination metadata for leaderboard responses.
/// </summary>
public class PaginationMetadata
{
    /// <summary>
    /// Total number of participants in the challenge.
    /// </summary>
    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page number.
    /// </summary>
    [JsonPropertyName("currentPage")]
    public int CurrentPage { get; set; }

    /// <summary>
    /// Number of entries per page.
    /// </summary>
    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }

    /// <summary>
    /// Total number of pages.
    /// </summary>
    [JsonPropertyName("totalPages")]
    public int TotalPages { get; set; }

    /// <summary>
    /// Whether there is a next page.
    /// </summary>
    [JsonPropertyName("hasNextPage")]
    public bool HasNextPage { get; set; }

    /// <summary>
    /// Whether there is a previous page.
    /// </summary>
    [JsonPropertyName("hasPreviousPage")]
    public bool HasPreviousPage { get; set; }
}
