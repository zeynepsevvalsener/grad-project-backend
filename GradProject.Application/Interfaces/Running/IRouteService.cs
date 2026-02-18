using GradProject.Application.DTOs.Running;

namespace GradProject.Application.Interfaces.Running
{
    /// <summary>
    /// Service for retrieving running activity routes in GeoJSON format.
    /// </summary>
    public interface IRouteService
    {
        /// <summary>
        /// Gets the route for a running activity as GeoJSON.
        /// Verifies that the activity belongs to the specified user.
        /// </summary>
        /// <param name="runId">The running activity ID</param>
        /// <param name="userId">The user ID to verify ownership</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>GeoJSON Feature, or null if route not found or activity doesn't belong to user</returns>
        Task<RouteGeoJsonResponseDto?> GetRouteGeoJsonAsync(int runId, int userId, CancellationToken ct = default);

        /// <summary>
        /// Checks if a running activity exists and belongs to the specified user.
        /// </summary>
        /// <param name="runId">The running activity ID</param>
        /// <param name="userId">The user ID to verify ownership</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>True if activity exists and belongs to user, false otherwise</returns>
        Task<bool> ActivityExistsAsync(int runId, int userId, CancellationToken ct = default);

        /// <summary>
        /// Gets the bounding box metadata for a running activity.
        /// </summary>
        /// <param name="runId">The running activity ID</param>
        /// <param name="userId">The user ID to verify ownership</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Bounding box metadata, or null if activity not found or doesn't belong to user</returns>
        Task<BoundingBoxMetadataDto?> GetBoundingBoxMetadataAsync(int runId, int userId, CancellationToken ct = default);

        /// <summary>
        /// Backfills bounding box and convex hull for a running activity that has a route but no bounding box.
        /// </summary>
        /// <param name="runId">The running activity ID</param>
        /// <param name="userId">The user ID to verify ownership</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Backfill result, or null if activity not found, doesn't belong to user, or has no route</returns>
        Task<BackfillBoundingBoxResultDto?> BackfillBoundingBoxAsync(int runId, int userId, CancellationToken ct = default);
    }
}

