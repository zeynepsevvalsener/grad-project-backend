using GradProject.Application.DTOs.Gamification;
using GradProject.Application.Interfaces.Gamification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GradProject.Api.Controllers;

/// <summary>
/// HLN-8: Claim and defend territory ownership by run.
/// </summary>
[Route("api/v1/territory")]
[Authorize]
public class TerritoryController : ApiControllerBase
{
    private readonly ITerritoryClaimDefendService _claimDefendService;
    private readonly ITerritoryCatalogService _catalogService;
    private readonly ILogger<TerritoryController> _logger;

    public TerritoryController(
        ITerritoryClaimDefendService claimDefendService,
        ITerritoryCatalogService catalogService,
        ILogger<TerritoryController> logger)
    {
        _claimDefendService = claimDefendService;
        _catalogService = catalogService;
        _logger = logger;
    }

    /// <summary>
    /// All territories with geometry cells and current owner (public catalog).
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<TerritoryCatalogItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TerritoryCatalogItemDto>>> GetCatalog(CancellationToken ct = default)
    {
        var items = await _catalogService.GetCatalogAsync(ct);
        return Ok(items);
    }

    /// <summary>
    /// Current user&apos;s progress for every territory (defaults to Locked / 0% when no row exists).
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(IReadOnlyList<MyTerritoryProgressRowDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<MyTerritoryProgressRowDto>>> GetMyProgress(CancellationToken ct = default)
    {
        var userId = GetUserIdOrThrow();
        var rows = await _catalogService.GetMyProgressAsync(userId, ct);
        return Ok(rows);
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

        var internalIds = await _catalogService.ResolvePublicIdsAsync(request.TerritoryIds, ct);
        if (internalIds.Count == 0)
            return BadRequest(new { reason = "TERRITORY_IDS_NOT_FOUND" });

        try
        {
            var result = await _claimDefendService.ClaimAsync(userId, request.RunId, internalIds, ct);

            if (result.ClaimedTerritories.Count > 0)
                _catalogService.InvalidateCatalogCache();

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

        var internalIds = await _catalogService.ResolvePublicIdsAsync(request.TerritoryIds, ct);
        if (internalIds.Count == 0)
            return BadRequest(new { reason = "TERRITORY_IDS_NOT_FOUND" });

        try
        {
            var result = await _claimDefendService.DefendAsync(userId, request.RunId, internalIds, ct);

            if (result.DefendedTerritories.Count > 0)
                _catalogService.InvalidateCatalogCache();

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

}
