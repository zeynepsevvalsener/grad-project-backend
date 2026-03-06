using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GradProject.Application.DTOs.Gamification;
using GradProject.Application.Interfaces.Gamification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GradProject.Api.Controllers;

/// <summary>
/// HLN-8: Claim and defend territory ownership by run.
/// </summary>
[ApiController]
[Route("api/v1/territory")]
[Authorize]
public class TerritoryController : ControllerBase
{
    private readonly ITerritoryClaimDefendService _claimDefendService;
    private readonly ILogger<TerritoryController> _logger;

    public TerritoryController(
        ITerritoryClaimDefendService claimDefendService,
        ILogger<TerritoryController> logger)
    {
        _claimDefendService = claimDefendService;
        _logger = logger;
    }

    /// <summary>
    /// Claim territory(ies) covered by a run. Unclaimed territories are claimed; occupied ones can be taken over (invade) if your score exceeds the current owner's snapshot.
    /// </summary>
    /// <param name="request">Run ID and list of territory IDs to claim</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Claimed territories, rejected with reasons, ownership updates, and events to emit</returns>
    [HttpPost("claim")]
    [ProducesResponseType(typeof(ClaimTerritoryResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClaimTerritoryResponseDto>> Claim(
        [FromBody] ClaimTerritoryRequestDto request,
        CancellationToken ct = default)
    {
        var userId = GetUserIdOrThrow();
        if (request.RunId <= 0)
            return BadRequest(new { reason = "RUN_ID_INVALID" });
        if (request.TerritoryIds == null || request.TerritoryIds.Count == 0)
            return BadRequest(new { reason = "TERRITORY_IDS_REQUIRED" });

        try
        {
            var result = await _claimDefendService.ClaimAsync(
                userId,
                request.RunId,
                request.TerritoryIds,
                ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex) when (ex.Message == "RUN_NOT_FOUND")
        {
            return NotFound(new { reason = "RUN_NOT_FOUND" });
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(403, new { reason = "RUN_NOT_OWNED_BY_USER" });
        }
    }

    /// <summary>
    /// Defend territory(ies) you own by updating the score snapshot with a new run. Only the current owner can defend.
    /// </summary>
    /// <param name="request">Run ID and list of territory IDs to defend</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Defended territories, rejected with reasons, updated score snapshots, and events to emit</returns>
    [HttpPost("defend")]
    [ProducesResponseType(typeof(DefendTerritoryResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DefendTerritoryResponseDto>> Defend(
        [FromBody] DefendTerritoryRequestDto request,
        CancellationToken ct = default)
    {
        var userId = GetUserIdOrThrow();
        if (request.RunId <= 0)
            return BadRequest(new { reason = "RUN_ID_INVALID" });
        if (request.TerritoryIds == null || request.TerritoryIds.Count == 0)
            return BadRequest(new { reason = "TERRITORY_IDS_REQUIRED" });

        try
        {
            var result = await _claimDefendService.DefendAsync(
                userId,
                request.RunId,
                request.TerritoryIds,
                ct);
            return Ok(result);
        }
        catch (InvalidOperationException ex) when (ex.Message == "RUN_NOT_FOUND")
        {
            return NotFound(new { reason = "RUN_NOT_FOUND" });
        }
        catch (UnauthorizedAccessException)
        {
            return StatusCode(403, new { reason = "RUN_NOT_OWNED_BY_USER" });
        }
    }

    private int GetUserIdOrThrow()
    {
        var sub = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                  ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(sub) || !int.TryParse(sub, out var userId))
            throw new UnauthorizedAccessException("Invalid token.");

        return userId;
    }
}
