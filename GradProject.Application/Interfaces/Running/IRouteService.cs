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
    }
}

