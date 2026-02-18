namespace GradProject.Application.Interfaces
{
    /// <summary>
    /// Service for managing Strava integration and user connections.
    /// </summary>
    public interface IStravaService
    {
        /// <summary>
        /// Saves Strava token information to user after OAuth callback.
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="accessToken">Strava access token</param>
        /// <param name="refreshToken">Strava refresh token</param>
        /// <param name="expiresAt">Token expiration time</param>
        /// <param name="athleteId">Strava athlete ID</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>True if successful, false if user not found</returns>
        Task<bool> SaveStravaTokenAsync(
            int userId,
            string accessToken,
            string refreshToken,
            DateTime expiresAt,
            long athleteId,
            CancellationToken ct = default);

        /// <summary>
        /// Gets Strava access token for a user.
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Access token, or null if user not found or not connected</returns>
        Task<string?> GetStravaAccessTokenAsync(int userId, CancellationToken ct = default);
    }
}

