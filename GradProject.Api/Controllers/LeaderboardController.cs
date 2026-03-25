using GradProject.Application.DTOs.Leaderboard;
using GradProject.Application.Interfaces.Leaderboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Npgsql;

namespace GradProject.Api.Controllers;

/// <summary>
/// Controller for leaderboard operations in challenges.
/// </summary>
[Route("api/v1/challenges")]
[Authorize]
public class LeaderboardController : ApiControllerBase
{
    private readonly ILeaderboardService _leaderboardService;
    private readonly ILogger<LeaderboardController> _logger;

    public LeaderboardController(
        ILeaderboardService leaderboardService,
        ILogger<LeaderboardController> logger)
    {
        _leaderboardService = leaderboardService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves the leaderboard for a specific challenge with pagination support.
    /// Returns ranked participants based on multi-criteria sorting: Territory Score, Total Distance, Average Pace, Completion Speed, and User ID.
    /// </summary>
    /// <param name="challengeId">The ID of the challenge to retrieve leaderboard for</param>
    /// <param name="page">Page number for pagination (1-based, default: 1, must be >= 1)</param>
    /// <param name="pageSize">Number of entries per page (default: 20, range: 1-100)</param>
    /// <param name="userId">Optional user ID to include in response regardless of pagination. Returns 404 if user not participating.</param>
    /// <param name="limit">Optional limit to retrieve only top N users (must be >= 1). When provided, ignores pagination.</param>
    /// <param name="ct">Cancellation token for request cancellation</param>
    /// <returns>
    /// 200 OK: Leaderboard response with entries, pagination metadata, and optional current user entry
    /// 400 Bad Request: Invalid query parameters (page < 1, pageSize out of range, limit < 1)
    /// 404 Not Found: Challenge not found or user not participating in challenge
    /// 503 Service Unavailable: Database temporarily unavailable
    /// </returns>
    [HttpGet("{challengeId}/leaderboard")]
    [EnableRateLimiting("LeaderboardPolicy")]
    public async Task<ActionResult<LeaderboardResponseDto>> GetLeaderboard(
        int challengeId,
        [FromQuery] int? page = null,
        [FromQuery] int? pageSize = null,
        [FromQuery] int? userId = null,
        [FromQuery] int? limit = null,
        [FromQuery] string? period = null,
        CancellationToken ct = default)
    {
        try
        {
            // Apply default values for optional parameters
            var actualPage = page ?? 1;
            var actualPageSize = pageSize ?? 20;

            _logger.LogInformation(
                "Retrieving leaderboard for challenge {ChallengeId} with page={Page}, pageSize={PageSize}, userId={UserId}, limit={Limit}",
                challengeId, actualPage, actualPageSize, userId, limit);

            var result = await _leaderboardService.GetLeaderboardAsync(
                challengeId,
                actualPage,
                actualPageSize,
                userId,
                limit,
                period,
                ct);

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            // Validation exceptions - return 400 Bad Request
            // Triggered by: page < 1, pageSize out of range (1-100), limit < 1
            _logger.LogWarning(ex,
                "Validation error for leaderboard request: challengeId={ChallengeId}, page={Page}, pageSize={PageSize}, userId={UserId}, limit={Limit}",
                challengeId, page, pageSize, userId, limit);

            return BadRequest(new
            {
                error = new
                {
                    message = ex.Message,
                    code = "VALIDATION_ERROR"
                }
            });
        }
        catch (KeyNotFoundException ex)
        {
            // Not found exceptions - return 404 Not Found
            // Triggered by: challenge doesn't exist, user not participating in challenge
            _logger.LogWarning(ex,
                "Resource not found for leaderboard request: challengeId={ChallengeId}, userId={UserId}",
                challengeId, userId);

            return NotFound(new
            {
                error = new
                {
                    message = ex.Message,
                    code = "NOT_FOUND"
                }
            });
        }
        catch (NpgsqlException ex)
        {
            // Database exceptions - return 503 Service Unavailable
            // Triggered by: database connection failure, timeout, infrastructure issues
            _logger.LogError(ex,
                "Database error while retrieving leaderboard: challengeId={ChallengeId}, page={Page}, pageSize={PageSize}, userId={UserId}, limit={Limit}",
                challengeId, page, pageSize, userId, limit);

            return StatusCode(503, new
            {
                error = new
                {
                    message = "Database service is temporarily unavailable. Please try again later.",
                    code = "SERVICE_UNAVAILABLE"
                }
            });
        }
        catch (Exception ex)
        {
            // Unexpected exceptions - return 500 Internal Server Error
            // Catch-all for any unhandled exceptions
            _logger.LogError(ex,
                "Unexpected error while retrieving leaderboard: challengeId={ChallengeId}, page={Page}, pageSize={PageSize}, userId={UserId}, limit={Limit}",
                challengeId, page, pageSize, userId, limit);

            return StatusCode(500, new
            {
                error = new
                {
                    message = "An unexpected error occurred. Please try again later.",
                    code = "INTERNAL_ERROR"
                }
            });
        }
    }
}
