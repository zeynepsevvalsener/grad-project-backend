using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GradProject.Application.DTOs.Running;
using GradProject.Application.Interfaces.Running;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GradProject.Api.Controllers
{
    [ApiController]
    [Route("api/v1/runs")]
    [Authorize]
    public class RunsController : ControllerBase
    {
        private readonly IRouteService _routeService;

        public RunsController(IRouteService routeService)
        {
            _routeService = routeService;
        }

        /// <summary>
        /// Gets the route for a running activity as GeoJSON.
        /// </summary>
        /// <param name="id">The running activity ID</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>GeoJSON Feature representing the route, 204 if route not available, or 404 if activity not found</returns>
        [HttpGet("{id:int}/route")]
        [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
        [ProducesResponseType(typeof(RouteGeoJsonResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<RouteGeoJsonResponseDto>> GetRoute(int id, CancellationToken ct)
        {
            var userId = GetUserIdOrThrow();

            var route = await _routeService.GetRouteGeoJsonAsync(id, userId, ct);

            if (route == null)
            {
                // Check if activity exists to determine if it's 404 or 204
                var activityExists = await _routeService.ActivityExistsAsync(id, userId, ct);
                if (!activityExists)
                {
                    return NotFound(new { message = "Running activity not found or doesn't belong to you." });
                }
                
                return NoContent();
            }

            return Ok(route);
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
}

