namespace GradProject.Application.Interfaces.Leaderboard;

using GradProject.Application.DTOs.Leaderboard;

/// <summary>
/// Service interface for leaderboard operations (challenge-scoped and global).
/// </summary>
public interface ILeaderboardService
{
    /// <summary>
    /// Retrieves paginated global leaderboard across all challenges and running activities.
    /// Eligible users: anyone with at least one UserChallenge or RunningActivity.
    /// </summary>
    Task<LeaderboardResponseDto> GetGlobalLeaderboardAsync(
        int page,
        int pageSize,
        int? userId,
        int? limit,
        string? period = null,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieves paginated leaderboard for a challenge
    /// </summary>
    /// <param name="challengeId">The ID of the challenge</param>
    /// <param name="page">Page number (1-based)</param>
    /// <param name="pageSize">Number of entries per page</param>
    /// <param name="userId">Optional viewer user ID; when set, <see cref="LeaderboardResponseDto.CurrentUserEntry"/> is filled if that user appears in the ranked list. Non-participants may be passed; entry stays null.</param>
    /// <param name="limit">Optional limit to retrieve only top N users</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Leaderboard response with entries, pagination metadata, and current user entry when applicable</returns>
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
    /// Runs platform-wide aggregation and ranking, then stores a global leaderboard snapshot
    /// (ChallengeId = null) for today. Idempotent: replaces any existing global snapshot for today.
    /// </summary>
    Task RefreshGlobalLeaderboardAsync(CancellationToken ct = default);

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
