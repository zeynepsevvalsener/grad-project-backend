namespace GradProject.Application.Interfaces.Leaderboard;

using GradProject.Application.DTOs.Leaderboard;

/// <summary>
/// Service interface for leaderboard operations in challenges
/// </summary>
public interface ILeaderboardService
{
    /// <summary>
    /// Retrieves paginated leaderboard for a challenge
    /// </summary>
    /// <param name="challengeId">The ID of the challenge</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of entries per page</param>
    /// <param name="userId">Optional user ID to include in response regardless of pagination</param>
    /// <param name="limit">Optional limit to retrieve only top N users</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Leaderboard response with entries, pagination metadata, and optional current user entry</returns>
    Task<LeaderboardResponseDto> GetLeaderboardAsync(
        int challengeId,
        int page,
        int pageSize,
        int? userId,
        int? limit,
        string? period = null,
        CancellationToken ct = default);

    /// <summary>
    /// Runs aggregation and ranking for a challenge (e.g. for daily refresh). No response returned.
    /// </summary>
    Task RefreshLeaderboardAsync(int challengeId, CancellationToken ct = default);

    /// <summary>
    /// Calculates individual user rank in a challenge
    /// </summary>
    /// <param name="challengeId">The ID of the challenge</param>
    /// <param name="userId">The ID of the user</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Leaderboard entry with the user's rank and metrics</returns>
    Task<LeaderboardEntryDto> GetUserRankAsync(
        int challengeId,
        int userId,
        CancellationToken ct = default);
}
