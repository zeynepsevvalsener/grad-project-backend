namespace GradProject.Application.DTOs.Leaderboard
{
    /// <summary>
    /// Query parameters for leaderboard requests
    /// </summary>
    public class LeaderboardQuery
    {
        /// <summary>
        /// Page number for pagination (must be >= 1)
        /// </summary>
        public int? Page { get; set; }

        /// <summary>
        /// Number of entries per page (must be between 1 and 100)
        /// </summary>
        public int? PageSize { get; set; }

        /// <summary>
        /// Maximum number of top users to retrieve (must be >= 1)
        /// </summary>
        public int? Limit { get; set; }

        /// <summary>
        /// User ID to include in the response regardless of pagination
        /// </summary>
        public int? UserId { get; set; }
    }
}
