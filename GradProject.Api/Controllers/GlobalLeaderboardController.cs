using GradProject.Application.DTOs.Leaderboard;
using GradProject.Application.Interfaces.Leaderboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Npgsql;

namespace GradProject.Api.Controllers;

/// <summary>
/// Controller for global leaderboard and challenge leaderboard alias routes.
/// Global: GET api/v1/leaderboard
/// Challenge alias: GET api/v1/leaderboard/challenge/{challengeId}
/// </summary>
[Route("api/v1/leaderboard")]
[Authorize]
public class GlobalLeaderboardController : ApiControllerBase
{
    private readonly ILeaderboardService _leaderboardService;
    private readonly ILogger<GlobalLeaderboardController> _logger;

    public GlobalLeaderboardController(
        ILeaderboardService leaderboardService,
        ILogger<GlobalLeaderboardController> logger)
    {
        _leaderboardService = leaderboardService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves the global leaderboard across all challenges and running activities.
    /// Eligible users: anyone with at least one challenge participation or running activity.
    /// Ranking: territory score DESC, total distance DESC, pace ASC, completion speed ASC, userId ASC.
    /// </summary>
    [HttpGet]
    [EnableRateLimiting("LeaderboardPolicy")]
    public async Task<ActionResult<LeaderboardResponseDto>> GetGlobalLeaderboard(
        [FromQuery] int? page = null,
        [FromQuery] int? pageSize = null,
        [FromQuery] int? userId = null,
        [FromQuery] int? limit = null,
        [FromQuery] string? period = null,
        CancellationToken ct = default)
    {
        try
        {
            var actualPage = page ?? 1;
            var actualPageSize = pageSize ?? 20;

            _logger.LogInformation(
                "Retrieving global leaderboard: page={Page}, pageSize={PageSize}, userId={UserId}, limit={Limit}",
                actualPage, actualPageSize, userId, limit);

            var result = await _leaderboardService.GetGlobalLeaderboardAsync(
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
            _logger.LogWarning(ex,
                "Validation error for global leaderboard: page={Page}, pageSize={PageSize}, userId={UserId}, limit={Limit}",
                page, pageSize, userId, limit);

            return BadRequest(new { error = new { message = ex.Message, code = "VALIDATION_ERROR" } });
        }
        catch (NpgsqlException ex)
        {
            _logger.LogError(ex,
                "Database error while retrieving global leaderboard: page={Page}, pageSize={PageSize}, userId={UserId}, limit={Limit}",
                page, pageSize, userId, limit);

            return StatusCode(503, new
            {
                error = new { message = "Database service is temporarily unavailable. Please try again later.", code = "SERVICE_UNAVAILABLE" }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error while retrieving global leaderboard: page={Page}, pageSize={PageSize}, userId={UserId}, limit={Limit}",
                page, pageSize, userId, limit);

            return StatusCode(500, new
            {
                error = new { message = "An unexpected error occurred. Please try again later.", code = "INTERNAL_ERROR" }
            });
        }
    }

    /// <summary>
    /// Alias route for challenge leaderboard: GET api/v1/leaderboard/challenge/{challengeId}.
    /// Delegates to the same service as GET api/v1/challenges/{challengeId}/leaderboard.
    /// </summary>
    [HttpGet("challenge/{challengeId}")]
    [EnableRateLimiting("LeaderboardPolicy")]
    public async Task<ActionResult<LeaderboardResponseDto>> GetChallengeLeaderboard(
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
            var actualPage = page ?? 1;
            var actualPageSize = pageSize ?? 20;

            _logger.LogInformation(
                "Retrieving challenge leaderboard (alias) for challenge {ChallengeId}: page={Page}, pageSize={PageSize}, userId={UserId}, limit={Limit}",
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
            _logger.LogWarning(ex,
                "Validation error for challenge leaderboard (alias): challengeId={ChallengeId}, page={Page}, pageSize={PageSize}, userId={UserId}, limit={Limit}",
                challengeId, page, pageSize, userId, limit);

            return BadRequest(new { error = new { message = ex.Message, code = "VALIDATION_ERROR" } });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex,
                "Resource not found for challenge leaderboard (alias): challengeId={ChallengeId}, userId={UserId}",
                challengeId, userId);

            return NotFound(new { error = new { message = ex.Message, code = "NOT_FOUND" } });
        }
        catch (NpgsqlException ex)
        {
            _logger.LogError(ex,
                "Database error while retrieving challenge leaderboard (alias): challengeId={ChallengeId}, page={Page}, pageSize={PageSize}, userId={UserId}, limit={Limit}",
                challengeId, page, pageSize, userId, limit);

            return StatusCode(503, new
            {
                error = new { message = "Database service is temporarily unavailable. Please try again later.", code = "SERVICE_UNAVAILABLE" }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error while retrieving challenge leaderboard (alias): challengeId={ChallengeId}, page={Page}, pageSize={PageSize}, userId={UserId}, limit={Limit}",
                challengeId, page, pageSize, userId, limit);

            return StatusCode(500, new
            {
                error = new { message = "An unexpected error occurred. Please try again later.", code = "INTERNAL_ERROR" }
            });
        }
    }
}
