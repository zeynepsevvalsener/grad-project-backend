using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GradProject.Application.DTOs.Running;
using GradProject.Application.Interfaces.Geometry;
using GradProject.Application.Interfaces.Running;
using GradProject.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GradProject.Api.Controllers
{
    [ApiController]
    [Route("api/v1/runs")]
    [Authorize]
    public class RunsController : ControllerBase
    {
        private readonly IRouteService _routeService;
        private readonly AppDbContext _db;
        private readonly IBoundingBoxService _boundingBoxService;
        private readonly IConvexHullService _convexHullService;

        public RunsController(IRouteService routeService, AppDbContext db, IBoundingBoxService boundingBoxService, IConvexHullService convexHullService)
        {
            _routeService = routeService;
            _db = db;
            _boundingBoxService = boundingBoxService;
            _convexHullService = convexHullService;
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

        /// <summary>
        /// Gets the bounding box metadata for a running activity.
        /// Useful for debugging and validating bounding box extraction.
        /// </summary>
        /// <param name="id">The running activity ID</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Bounding box metadata, 204 if no bounding box, or 404 if activity not found</returns>
        [HttpGet("{id:int}/bounding-box")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> GetBoundingBox(int id, CancellationToken ct)
        {
            var userId = GetUserIdOrThrow();

            var activity = await _db.RunningActivities
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId, ct);

            if (activity == null)
            {
                return NotFound(new { message = "Running activity not found or doesn't belong to you." });
            }

            if (activity.MinLat == null || activity.MaxLat == null || 
                activity.MinLng == null || activity.MaxLng == null)
            {
                return NoContent();
            }

            return Ok(new
            {
                runId = activity.Id,
                runName = activity.Name,
                hasRoute = activity.Route != null,
                boundingBox = new
                {
                    minLat = activity.MinLat,
                    maxLat = activity.MaxLat,
                    minLng = activity.MinLng,
                    maxLng = activity.MaxLng,
                    centerLat = (activity.MinLat + activity.MaxLat) / 2.0,
                    centerLng = (activity.MinLng + activity.MaxLng) / 2.0,
                    width = activity.MaxLng - activity.MinLng,
                    height = activity.MaxLat - activity.MinLat
                },
                validation = new
                {
                    isValid = activity.MinLat <= activity.MaxLat && activity.MinLng <= activity.MaxLng,
                    minLatLessThanMaxLat = activity.MinLat <= activity.MaxLat,
                    minLngLessThanMaxLng = activity.MinLng <= activity.MaxLng
                }
            });
        }

        /// <summary>
        /// Backfills bounding box for a running activity that has a route but no bounding box.
        /// Useful for migrating existing data after bounding box feature was added.
        /// </summary>
        /// <param name="id">The running activity ID</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Updated bounding box metadata, 204 if no route, or 404 if activity not found</returns>
        [HttpPost("{id:int}/bounding-box/backfill")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult> BackfillBoundingBox(int id, CancellationToken ct)
        {
            var userId = GetUserIdOrThrow();

            var activity = await _db.RunningActivities
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId, ct);

            if (activity == null)
            {
                return NotFound(new { message = "Running activity not found or doesn't belong to you." });
            }

            if (activity.Route == null)
            {
                return NoContent();
            }

            // Extract bounding box and convex hull from existing route
            var bbox = _boundingBoxService.Extract(activity.Route);
            var convexHull = _convexHullService.Extract(activity.Route);

            var hasBbox = bbox != null && bbox.MinLat <= bbox.MaxLat && bbox.MinLng <= bbox.MaxLng;
            var hasConvexHull = convexHull != null && !convexHull.IsEmpty && convexHull.IsValid;

            // At least one must succeed
            if (!hasBbox && !hasConvexHull)
            {
                return BadRequest(new { message = "Failed to extract valid bounding box or convex hull from route." });
            }

            var updated = false;

            if (hasBbox)
            {
                activity.MinLat = bbox.MinLat;
                activity.MaxLat = bbox.MaxLat;
                activity.MinLng = bbox.MinLng;
                activity.MaxLng = bbox.MaxLng;
                updated = true;
            }

            if (hasConvexHull)
            {
                activity.ConvexHull = convexHull;
                updated = true;
            }

            if (updated)
            {
                activity.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(ct);
            }

            // Reload from database to ensure ConvexHull is loaded
            await _db.Entry(activity).ReloadAsync(ct);

            return Ok(new
            {
                runId = activity.Id,
                runName = activity.Name,
                message = "Region metadata backfilled successfully",
                boundingBox = hasBbox ? new
                {
                    minLat = activity.MinLat,
                    maxLat = activity.MaxLat,
                    minLng = activity.MinLng,
                    maxLng = activity.MaxLng,
                    centerLat = (activity.MinLat + activity.MaxLat) / 2.0,
                    centerLng = (activity.MinLng + activity.MaxLng) / 2.0,
                    width = activity.MaxLng - activity.MinLng,
                    height = activity.MaxLat - activity.MinLat
                } : null,
                convexHull = hasConvexHull ? new
                {
                    available = true,
                    numPoints = convexHull?.ExteriorRing?.NumPoints ?? 0,
                    isValid = convexHull?.IsValid ?? false,
                    isEmpty = convexHull?.IsEmpty ?? true
                } : null
            });
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

